using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Nito.AsyncEx;
using STAN.Client;

namespace Sitko.Core.PersistentQueue.Nats
{
    public class NatsConnectionFactory : IPersistentQueueConnectionFactory<NatsQueueConnection>
    {
        private readonly NatsPersistentQueueModuleOptions _options;
        private readonly ILogger<NatsConnectionFactory> _logger;
        private NatsQueueConnection _connection;
        private readonly AsyncLock _locker = new AsyncLock();
        private static readonly ConnectionFactory Cf = new ConnectionFactory();

        public NatsConnectionFactory(NatsPersistentQueueModuleOptions options,
            ILogger<NatsConnectionFactory> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<NatsQueueConnection> GetConnection()
        {
            if (_connection == null)
            {
                _logger.LogInformation("No connection. Create new");
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    using (await _locker.LockAsync(cts.Token))
                    {
                        _connection = CreateConnection();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw new Exception("Can't create connection: lock timeout");
                }
            }

            return _connection;
        }

        public NatsQueueConnection[] GetCurrentConnections()
        {
            return new[] {_connection};
        }

        private NatsQueueConnection CreateConnection()
        {
            IConnection natsConn = null;
            var clientId = $"{_options.ClientName}_{Guid.NewGuid()}";
            try
            {
                natsConn = GetNatsConnection(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Nats connection error ({exType}): {errorText}. Connection error: {connectionError} - {connectionInnerError}. Nats urls: {natsUrls}. Nats timeout: {natsTimeout}",
                    ex.GetType(), ex.ToString(), natsConn?.LastError.ToString(),
                    natsConn?.LastError?.InnerException?.ToString(), _options.Servers, _options.ConnectionTimeout);
                try
                {
                    natsConn?.Close();
                }
                catch (Exception)
                {
                    _logger.LogError(ex, ex.Message);
                }

                throw;
            }

            if (natsConn.State != ConnState.CONNECTED)
                throw new Exception("nats conn is not connected");
            try
            {
                var options = StanOptions.GetDefaultOptions();
                options.NatsConn = natsConn;
                options.ConnectTimeout = _options.ConnectionTimeout;
                var cf = new StanConnectionFactory();
                var stanConnection = cf.CreateConnection(_options.ClusterName, clientId, options);
                if (stanConnection.NATSConnection.State == ConnState.CONNECTED)
                {
                    var connection = new NatsQueueConnection(stanConnection, natsConn, _options);
                    return connection;
                }

                throw new Exception("nats conn is not connected");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Error while connecting to nats: {errorText}. Connection error: {connectionError} - {connectionInnerError}",
                    ex.ToString(), natsConn.LastError?.ToString(), natsConn.LastError?.InnerException?.ToString());
                throw;
            }
        }

        private IConnection GetNatsConnection(string clientId)
        {
            var opts = GetOptions();
            opts.Name = clientId;
            return Cf.CreateConnection(opts);
        }

        private Options GetOptions()
        {
            var opts = ConnectionFactory.GetDefaultOptions();
            opts.AllowReconnect = true;
            opts.PingInterval = 1000;
            opts.MaxPingsOut = 3;
            opts.AsyncErrorEventHandler =
                (sender, args) =>
                {
                    _logger.LogError(
                        "NATS event error: {errorText}. Connection {connection}. Subs: {subscription}", args.Error,
                        args.Conn, args.Subscription);
                };
            opts.ClosedEventHandler =
                (sender, args) => { _logger.LogInformation("Stan connection closed: {conn}", args.Conn); };
            opts.DisconnectedEventHandler =
                (sender, args) => { _logger.LogInformation("NATS connection disconnected: {conn}", args.Conn); };
            opts.ReconnectedEventHandler =
                (sender, args) =>
                {
                    _logger.LogInformation("NATS connection reconnected: {conn}", args.Conn);
                };
            if (_options.Servers.Any())
            {
                var servers = new List<string>();
                foreach (var server in _options.Servers)
                {
                    if (IPAddress.TryParse(server.host, out var ip))
                    {
                        servers.Add($"nats://{ip}:{server.port}");
                    }
                    else
                    {
                        var entry = Dns.GetHostEntry(server.host);
                        if (entry.AddressList.Any())
                        {
                            foreach (var ipAddress in entry.AddressList)
                            {
                                servers.Add($"nats://{ipAddress}:{server.port}");
                            }
                        }
                        else
                        {
                            throw new Exception($"Can't resolve ip for host {server.host}");
                        }
                    }
                }

                if (_options.Verbose)
                    _logger.LogInformation("Nats urls: {urls}", servers);
                opts.Servers = servers.ToArray();
            }

            opts.Verbose = _options.Verbose;
            opts.Timeout = _options.ConnectionTimeout;
            if (_options.Verbose)
                _logger.LogInformation("Nats timeout: {timeout}", _options.ConnectionTimeout);

            return opts;
        }

        public NatsQueueConnection GetCurrentConnection()
        {
            return _connection;
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogInformation("PQ Connection factory closing");
                _connection?.Dispose();
                _logger.LogInformation("PQ Connection factory closed");
                _disposed = true;
            }
        }
    }
}
