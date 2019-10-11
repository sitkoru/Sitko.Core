using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Nito.AsyncEx;
using Sitko.Core.PersistentQueue.Common;

namespace Sitko.Core.PersistentQueue.Internal
{
    public class PooledPersistentQueueConnectionFactory : IPersistentQueueConnectionFactory, IDisposable
    {
        private readonly ConcurrentStack<PersistentQueueConnection> _connections = new ConcurrentStack<PersistentQueueConnection>();
        private readonly ILogger<PooledPersistentQueueConnectionFactory> _logger;
        private readonly PersistentQueueMetricsCollector _metricsCollector;
        private readonly AsyncLock _locker = new AsyncLock();
        private readonly PersistentQueueOptions _options;
        private readonly CancellationTokenSource _pruneTaskCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _reconnectTaskCts = new CancellationTokenSource();
        private readonly List<PersistentQueueConnector> _connectors = new List<PersistentQueueConnector>();

        public PersistentQueueConnection[] GetConnections()
        {
            return _connections.ToArray();
        }

        public PersistentQueueConnector[] GetConnectors()
        {
            return _connectors.ToArray();
        }

        public PooledPersistentQueueConnectionFactory(PersistentQueueOptions options,
            ILogger<PooledPersistentQueueConnectionFactory> logger, PersistentQueueMetricsCollector metricsCollector)
        {
            _options = options;
            _logger = logger;
            _metricsCollector = metricsCollector;

#pragma warning disable 4014
            StartFillPoolTaskAsync();
            StartPruneTaskAsync();
            StartReconnectionTaskAsync();
#pragma warning restore 4014
        }

        private async Task StartFillPoolTaskAsync()
        {
            _logger.LogDebug("Fill pool");
            while (_connections.Count < _options.PoolMinSize)
            {
                try
                {
                    _connections.Push(await CreateConnectionAsync());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }

            _logger.LogDebug("Pool filled. Pool size: {poolSize}", _connections.Count);
        }

        private async Task StartReconnectionTaskAsync()
        {
            while (!_reconnectTaskCts.IsCancellationRequested)
            {
                await Task.Delay(_options.ReconnectInterval, _reconnectTaskCts.Token);
                await CheckFailedConnectionsAsync();
            }

            _logger.LogDebug("Reconnect task cancelled");
        }

        private async Task StartPruneTaskAsync()
        {
            while (!_pruneTaskCts.IsCancellationRequested)
            {
                await Task.Delay(_options.PruneInterval, _pruneTaskCts.Token);
                PruneIdleConnections();
            }

            _logger.LogDebug("Prune task cancelled");
        }

        private async Task CheckFailedConnectionsAsync()
        {
            foreach (var connector in _connectors)
            {
                var state = connector.Connection.NATSConnection.State;
                if (state == ConnState.CLOSED || state == ConnState.DISCONNECTED)
                {
                    try
                    {
                        connector.Connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.ToString());
                    }

                    _logger.LogWarning("Restore connection to Nats");
                    var connection = await AcquireConnectionAsync();
                    connector.Reconnect(connection);
                }
            }
        }

        private void PruneIdleConnections()
        {
            var count = _connections.Count;
            _logger.LogDebug("Connections pool size before prune: {count}", count);
            var processed = 0;
            while (processed < count && _connections.Count > _options.PoolMinSize &&
                   _connections.TryPop(out var connection))
            {
                if (DateTimeOffset.UtcNow - connection.LastReleaseDate <= _options.IdleTime)
                {
                    _connections.Push(connection);
                }
                else
                {
                    _logger.LogDebug("Close idle connection {connectionId}", connection.Id);
                    connection.Dispose();
                    _metricsCollector.TrackConnectionPrune();
                }

                processed++;
            }

            _logger.LogDebug("Connections pool size after prune: {poolSize}", _connections.Count);
        }

        public void Dispose()
        {
            _logger.LogInformation("PQ Connection factory closing");
            while (_connections.TryPop(out var connection))
                connection.Dispose();
            _pruneTaskCts.Cancel();
            _reconnectTaskCts.Cancel();
            _logger.LogInformation("PQ Connection factory closed");
        }


        public async Task<PersistentQueueConnector> GetConnectorAsync<T>() where T : IMessage
        {
            var connection = await AcquireConnectionAsync();

            var connector = new PersistentQueueConnector<T>(this, connection, _logger);
            _connectors.Add(connector);
            return connector;
        }

        private async Task<PersistentQueueConnection> AcquireConnectionAsync()
        {
            _connections.TryPop(out var connection);
            if (connection == null)
            {
                _logger.LogInformation("No connections in pool. Create new.");
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    using (await _locker.LockAsync(cts.Token))
                    {
                        connection = await CreateConnectionAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw new Exception("Can't create connection: lock timeout");
                }
            }
            else if (connection.Connection.NATSConnection.State != ConnState.CONNECTED)
            {
                _logger.LogWarning("Connection is broken, delete");
                connection.Dispose();
                return await AcquireConnectionAsync();
            }
            else
            {
                _logger.LogInformation(
                    "Got connection {connectionId} from pool. Connections left: {connectionsCount}", connection.Id,
                    _connections.Count);
                _metricsCollector.TrackPoolSize(_connections.Count);
            }

            connection.Take();
            return connection;
        }

        private async Task<PersistentQueueConnection> CreateConnectionAsync()
        {
            var connection = new PersistentQueueConnection(_options, _logger);
            await connection.ConnectAsync();
            return connection;
        }

        public void ReleaseConnector(PersistentQueueConnector connector, PersistentQueueConnection connection)
        {
            _connectors.Remove(connector);
            connection.Release();
            if (_connections.Count < _options.PoolMaxSize)
            {
                _connections.Push(connection);
                _metricsCollector.TrackPoolSize(_connections.Count);
                _logger.LogInformation(
                    "Connection {connectionId} returned to pool. Connections left: {connectionsCount}", connection.Id,
                    _connections.Count);
                _logger.LogInformation(
                    "Connection {connectionId} lifetime. Created: {connectionCreationDate}. Last use time: {useSeconds} seconds",
                    connection.Id, connection.CreationDate,
                    (connection.LastReleaseDate - connection.LastTakeDate).TotalSeconds);
                _metricsCollector.TrackConnectionUsageTime(
                    (long)(connection.LastReleaseDate - connection.LastTakeDate).TotalMilliseconds);
            }
            else
            {
                _metricsCollector.TrackPoolSize(_connections.Count);
            }
        }
    }
}
