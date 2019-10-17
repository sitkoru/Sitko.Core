using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Nito.AsyncEx;
using Sitko.Core.PersistentQueue.Common;

namespace Sitko.Core.PersistentQueue.Internal
{
    public class SinglePersistentQueueConnectionFactory : IPersistentQueueConnectionFactory, IDisposable
    {
        private PersistentQueueConnection _connection;
        private readonly List<PersistentQueueConnector> _connectors = new List<PersistentQueueConnector>();
        private readonly ILogger<SinglePersistentQueueConnectionFactory> _logger;
        private readonly PersistentQueueOptions _options;
        private readonly AsyncLock _locker = new AsyncLock();

        public PersistentQueueConnection[] GetConnections()
        {
            if (_connection != null)
            {
                return new[] {_connection};
            }

            return new PersistentQueueConnection[0];
        }

        public PersistentQueueConnector[] GetConnectors()
        {
            return _connectors.ToArray();
        }

        public SinglePersistentQueueConnectionFactory(PersistentQueueOptions options,
            ILogger<SinglePersistentQueueConnectionFactory> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void Dispose()
        {
            _logger.LogInformation("PQ Connection factory closing");
            _connectors.Clear();
            _connection?.Dispose();
            _logger.LogInformation("PQ Connection factory closed");
        }


        public async Task<PersistentQueueConnector> GetConnectorAsync<T>() where T : IMessage
        {
            if (_connection == null || _connection.Connection.NATSConnection.State != ConnState.CONNECTED)
            {
                await AcquireConnectionAsync();
                foreach (var pqConnector in _connectors)
                {
                    pqConnector.SetConnection(_connection);
                }
            }

            var connector = _connectors.FirstOrDefault(c => c.MessageType == typeof(T));
            if (connector == null)
            {
                connector = new PersistentQueueConnector<T>(this, _connection, _logger);
                _connectors.Add(connector);
            }

            return connector;
        }

        private async Task AcquireConnectionAsync()
        {
            if (_connection == null)
            {
                _logger.LogInformation("No connection. Create new");
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    using (await _locker.LockAsync(cts.Token))
                    {
                        _connection = await CreateConnectionAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw new Exception("Can't create connection: lock timeout");
                }
            }
        }

        private async Task<PersistentQueueConnection> CreateConnectionAsync()
        {
            var connection = new PersistentQueueConnection(_options, _logger);
            await connection.ConnectAsync();
            return connection;
        }

        public void ReleaseConnector(PersistentQueueConnector connector, PersistentQueueConnection connection)
        {
            _logger.LogDebug("Connector was disposed");
            _connectors.Remove(connector);
        }
    }
}
