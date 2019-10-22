using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Sitko.Core.PersistentQueue.Internal;
using STAN.Client;

namespace Sitko.Core.PersistentQueue.Common
{
    public class PersistentQueueConnection : IDisposable
    {
        private readonly PersistentQueueOptions _options;
        private readonly ILogger _logger;
        internal readonly DateTimeOffset CreationDate;
        private static readonly ConnectionFactory Cf = new ConnectionFactory();

        internal readonly Guid Id = Guid.NewGuid();
        private bool _isBusy;
        internal DateTimeOffset LastReleaseDate;
        internal DateTimeOffset LastTakeDate;
        private IConnection _natsConn;
        internal event ReconnectedEventHandler Reconnected;

        public PersistentQueueConnection(PersistentQueueOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
            CreationDate = DateTimeOffset.UtcNow;
        }

        public async Task ConnectAsync()
        {
            Connection?.Dispose();
            Connection = await GetConnectionAsync();
        }

        [SuppressMessage("ReSharper", "IDISP001")]
        private Task<IStanConnection> GetConnectionAsync()
        {
            if (_options.EmulationMode)
            {
                return Task.FromResult((IStanConnection)new PersistentQueueEmulatedConnection());
            }

            _natsConn = null;
            var clientId = $"{_options.ClientName}_{Guid.NewGuid()}";
            try
            {
                _natsConn = GetNatsConnection(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Nats connection error ({exType}): {errorText}. Connection error: {connectionError} - {connectionInnerError}. Nats urls: {natsUrls}. Nats timeout: {natsTimeout}",
                    ex.GetType(), ex.ToString(), _natsConn?.LastError.ToString(),
                    _natsConn?.LastError?.InnerException?.ToString(), _options.Servers, _options.ConnectionTimeout);
                try
                {
                    _natsConn?.Close();
                }
                catch (Exception)
                {
                    _logger.LogError(ex, ex.Message);
                }

                throw;
            }

            if (_natsConn.State != ConnState.CONNECTED)
                throw new Exception("nats conn is not connected");
            try
            {
                var options = StanOptions.GetDefaultOptions();
                options.NatsConn = _natsConn;
                options.ConnectTimeout = _options.ConnectionTimeout;
                var cf = new StanConnectionFactory();
                var connection = cf.CreateConnection(_options.ClusterName, clientId, options);
                if (connection.NATSConnection.State == ConnState.CONNECTED)
                    return Task.FromResult(connection);
                throw new Exception("nats conn is not connected");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Error while connecting to nats: {errorText}. Connection error: {connectionError} - {connectionInnerError}",
                    ex.ToString(), _natsConn.LastError?.ToString(), _natsConn.LastError?.InnerException?.ToString());
                throw;
            }
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
                    Reconnected?.Invoke(this, new ReconnectedEventHandlerArgs());
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

        private IConnection GetNatsConnection(string clientId)
        {
            var opts = GetOptions();
            opts.Name = clientId;
            return Cf.CreateConnection(opts);
        }

        internal IStanConnection Connection { get; private set; }

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
            _natsConn.Close();
            _natsConn.Dispose();
        }

        internal void Take()
        {
            if (_isBusy)
                throw new Exception($"Try to take busy connection {Id}");
            LastTakeDate = DateTimeOffset.UtcNow;
            _isBusy = true;
        }

        internal void Release()
        {
            LastReleaseDate = DateTimeOffset.UtcNow;
            _isBusy = false;
        }
    }
}
