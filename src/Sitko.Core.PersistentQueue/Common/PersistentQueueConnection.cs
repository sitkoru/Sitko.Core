using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
                var connection = cf.CreateConnection(_options.ClusterName, clientId, options);
                if (connection.NATSConnection.State == ConnState.CONNECTED)
                    return Task.FromResult(connection);
                throw new Exception("nats conn is not connected");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Error while connecting to nats: {errorText}. Connection error: {connectionError} - {connectionInnerError}", ex.ToString(), natsConn.LastError?.ToString(), natsConn.LastError?.InnerException?.ToString());
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
                        "NATS event error: {errorText}. Connection {connection}. Subs: {subscription}", args.Error, args.Conn, args.Subscription);
                };
            opts.ClosedEventHandler =
                (sender, args) => { _logger.LogError("Stan connection closed: {conn}", args.Conn); };
            opts.DisconnectedEventHandler =
                (sender, args) => { _logger.LogError("NATS connection disconnected: {conn}", args.Conn); };
            opts.ReconnectedEventHandler =
                (sender, args) =>
                {
                    _logger.LogError("NATS connection reconnected: {conn}", args.Conn);
                    Reconnected?.Invoke(this, new ReconnectedEventHandlerArgs());
                };
            if (_options.Servers.Any())
            {
                if (_options.Verbose)
                    _logger.LogInformation("Nats urls: {urls}", _options.Servers);
                opts.Servers = _options.Servers.ToArray();
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
