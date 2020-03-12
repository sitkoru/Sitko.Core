using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Newtonsoft.Json;
using Sitko.Core.Queue.Exceptions;
using Sitko.Core.Queue.Internal;
using STAN.Client;

namespace Sitko.Core.Queue.Nats
{
    public class NatsQueue : BaseQueue<NatsQueueModuleConfig>
    {
        private IStanConnection? _connection;
        private static readonly ConnectionFactory Cf = new ConnectionFactory();

        private readonly ConcurrentDictionary<Guid, IAsyncSubscription> _natsSubscriptions =
            new ConcurrentDictionary<Guid, IAsyncSubscription>();

        private readonly MethodInfo _deserializeBinaryMethod =
            typeof(NatsQueue).GetMethod(nameof(DeserializeBinaryPayload),
                BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly ConcurrentDictionary<string, IStanSubscription> _stanSubscriptions =
            new ConcurrentDictionary<string, IStanSubscription>();

        private bool _disposed;
        private IConnection? _natsConn;


        public NatsQueue(NatsQueueModuleConfig config, QueueContext context, ILogger<NatsQueue> logger) : base(config,
            context, logger)
        {
        }

        protected override Task DoStartAsync()
        {
            _connection = CreateConnection();
            return Task.CompletedTask;
        }

        private string GetQueueName<T>(T message) where T : class
        {
            string queueName = typeof(T).FullName;
            if (message is IMessage protoMessage)
            {
                queueName = protoMessage.Descriptor.FullName;
            }

            if (!string.IsNullOrEmpty(_config.QueueNamePrefix))
            {
                queueName = $"{_config.QueueNamePrefix}_{queueName}";
            }

            return queueName;
        }

        private string GetQueueName<T>() where T : class
        {
            return GetQueueName(Activator.CreateInstance<T>());
        }

        protected override async Task<QueuePublishResult> DoPublishAsync<T>(QueuePayload<T> queuePayload)
        {
            if (_connection == null)
            {
                throw new Exception("Connection to nats not etablished");
            }

            var result = new QueuePublishResult();
            try
            {
                await _connection.PublishAsync(GetQueueName(queuePayload.Message), SerializePayload(queuePayload));
            }
            catch (Exception e)
            {
                result.SetException(e);
            }

            return result;
        }

        protected override async Task<QueuePayload<TResponse>?> DoRequestAsync<TMessage, TResponse>(
            QueuePayload<TMessage> queuePayload, TimeSpan timeout)
        {
            if (_natsConn == null)
            {
                throw new Exception("Connection to nats not etablished");
            }

            try
            {
                var deserializer = GetPayloadDeserializer<TResponse>();
                var result =
                    await _natsConn.RequestAsync(GetQueueName(queuePayload.Message), SerializePayload(queuePayload),
                        (int)timeout.TotalMilliseconds);
                try
                {
                    return deserializer(result.Data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while deserializing response of type {Type}: {ErrorText}",
                        typeof(TResponse), ex.ToString());
                    return null;
                }
            }
            catch (NATSTimeoutException)
            {
                throw new QueueRequestTimeoutException(timeout);
            }
        }

        protected override Task<QueueSubscribeResult> DoReplyAsync<TMessage, TResponse>(
            Func<QueuePayload<TMessage>, Task<QueuePayload<TResponse>?>> callback)
        {
            if (_natsConn == null)
            {
                throw new Exception("Connection to nats not established");
            }

            var deserializer = GetPayloadDeserializer<TMessage>();
            if (deserializer == null)
            {
                throw new Exception($"Empty deserialize method for {typeof(TMessage)}");
            }

            var queue = GetQueueName<TMessage>();
            var id = Guid.NewGuid();
            var sub = _natsConn.SubscribeAsync(queue,
                async (sender, args) =>
                {
                    var request = deserializer(args.Message.Data);
                    var response = await callback(request);
                    if (response != null)
                    {
                        args.Message.Respond(SerializePayload(response));
                    }
                });
            _natsSubscriptions.TryAdd(id, sub);
            return Task.FromResult(new QueueSubscribeResult() {SubscriptionId = id});
        }

        protected override Task<bool> DoStopReplyAsync<TMessage, TResponse>(Guid id)
        {
            if (_natsSubscriptions.TryRemove(id, out var natsSubscription))
            {
                natsSubscription.Unsubscribe();
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private byte[] SerializePayload<T>(QueuePayload<T> payload) where T : class
        {
            var context = payload.MessageContext != null
                ? new QueueContextMsg
                {
                    Id = payload.MessageContext.Id.ToString(),
                    Date = payload.MessageContext.Date.ToTimestamp(),
                    MessageType = payload.MessageContext.MessageType ?? string.Empty
                }
                : new QueueContextMsg();
            if (!string.IsNullOrEmpty(payload.MessageContext?.RequestId))
            {
                context.RequestId = payload.MessageContext.RequestId;
            }

            if (payload.MessageContext?.ParentMessageId != null)
            {
                context.ParentMessageId = payload.MessageContext.ParentMessageId.ToString();
            }

            if (payload.MessageContext?.RootMessageId != null)
            {
                context.RootMessageId = payload.MessageContext.RootMessageId.ToString();
            }

            if (payload.MessageContext?.RootMessageDate != null)
            {
                context.RootMessageDate = payload.MessageContext.RootMessageDate.Value.ToTimestamp();
            }

            IMessage msg;
            if (payload.Message is IMessage protoMessage)
            {
                msg = new QueueBinaryMsg {Context = context, Data = Any.Pack(protoMessage)};
            }
            else
            {
                msg = new QueueJsonMsg {Context = context, Data = JsonConvert.SerializeObject(payload.Message)};
            }

            return msg.ToByteArray();
        }


        private QueueMessageContext DeserializeContext(QueueContextMsg contextMsg)
        {
            var context = new QueueMessageContext
            {
                Id = Guid.Parse(contextMsg.Id),
                Date = contextMsg.Date.ToDateTimeOffset(),
                MessageType = contextMsg.MessageType
            };
            if (!string.IsNullOrEmpty(contextMsg.ParentMessageId))
            {
                context.ParentMessageId = Guid.Parse(contextMsg.ParentMessageId);
            }

            if (!string.IsNullOrEmpty(contextMsg.RequestId))
            {
                context.RequestId = contextMsg.RequestId;
            }

            if (!string.IsNullOrEmpty(contextMsg.RootMessageId))
            {
                context.RootMessageId = Guid.Parse(contextMsg.RootMessageId);
                context.RootMessageDate = contextMsg.RootMessageDate.ToDateTimeOffset();
            }

            return context;
        }

        protected override Task<QueueSubscribeResult> DoSubscribeAsync<T>(IQueueMessageOptions<T>? options = null)
        {
            if (_connection == null)
            {
                throw new Exception("Connection to nats not established");
            }

            var result = new QueueSubscribeResult();
            var queueName = GetQueueName<T>();
            if (_stanSubscriptions.ContainsKey(queueName))
            {
                return Task.FromResult(result);
            }

            var stanOptions = StanSubscriptionOptions.GetDefaultOptions();
            if (!(options is NatsMessageOptions<T> queueOptions))
            {
                queueOptions = new NatsMessageOptions<T>();
            }

            if (queueOptions.All)
            {
                _logger.LogInformation("{QueueName}: Load all messages", queueName);
                stanOptions.DeliverAllAvailable();
                queueOptions.Durable = false;
            }
            else
            {
                if (queueOptions.StartAt.HasValue)
                {
                    _logger.LogInformation("{QueueName}: Load all messages starts from {Date}", queueName,
                        queueOptions.StartAt.Value);
                    stanOptions.StartAt(queueOptions.StartAt.Value);
                    queueOptions.Durable = false;
                }
            }

            if (queueOptions.ManualAck)
            {
                _logger.LogInformation("{QueueName}: Manual acks with {Timeout} timeout", queueName,
                    queueOptions.AckWait);
                stanOptions.AckWait = (int)queueOptions.AckWait.TotalMilliseconds;
                stanOptions.ManualAcks = true;
            }

            if (queueOptions.MaxInFlight > 0)
            {
                _logger.LogInformation("{QueueName}: {MaxInFlight} max in flight", queueName, queueOptions.MaxInFlight);
                stanOptions.MaxInflight = queueOptions.MaxInFlight;
            }

            if (queueOptions.Durable && !string.IsNullOrEmpty(_config.ConsumerGroupName))
            {
                _logger.LogInformation("{QueueName}: Durable name - {DurableName}", queueName,
                    _config.ConsumerGroupName);
                stanOptions.DurableName = _config.ConsumerGroupName;
            }


            var deserializer = GetPayloadDeserializer<T>();
            if (deserializer == null)
            {
                throw new Exception($"Empty deserialize method for {typeof(T)}");
            }

            IStanSubscription sub;
            if (!string.IsNullOrEmpty(stanOptions.DurableName))
            {
                sub = _connection.Subscribe(queueName,
                    _config.ConsumerGroupName, stanOptions,
                    async (sender, args) =>
                        await ProcessStanMessage(deserializer, args.Message, stanOptions.ManualAcks));
            }
            else
            {
                sub = _connection.Subscribe(queueName, stanOptions,
                    async (sender, args) =>
                        await ProcessStanMessage(deserializer, args.Message, stanOptions.ManualAcks));
            }

            _logger.LogInformation("{QueueName}: Subscribed", queueName);

            _stanSubscriptions.TryAdd(queueName, sub);

            return Task.FromResult(result);
        }

        private async Task ProcessStanMessage<T>(Func<byte[], QueuePayload<T>> deserializer, StanMsg message,
            bool manualAcks) where T : class
        {
            try
            {
                var payload = deserializer(message.Data);
                var processResult = await ProcessMessageAsync(payload);
                if (processResult && manualAcks)
                {
                    message.Ack();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing message of type {Type}: {ErrorText}", typeof(T),
                    ex.ToString());
            }
        }

        private Func<byte[], QueuePayload<T>> GetPayloadDeserializer<T>() where T : class
        {
            var isBinary = typeof(IMessage).IsAssignableFrom(typeof(T));
            Func<byte[], QueuePayload<T>> deserializer;
            if (isBinary)
            {
                var processMethod = _deserializeBinaryMethod.MakeGenericMethod(typeof(T));
                if (processMethod == null)
                {
                    throw new Exception($"Can't create generic deserialize method for {typeof(T)}");
                }

                deserializer = bytes => (QueuePayload<T>)processMethod.Invoke(this, new object[] {bytes});
            }
            else
            {
                deserializer = DeserializeJsonPayload<T>;
            }

            return deserializer;
        }

        private QueuePayload<T> DeserializeJsonPayload<T>(byte[] data) where T : class
        {
            var jsonMsg = new QueueJsonMsg();
            jsonMsg.MergeFrom(data);
            return new QueuePayload<T>(JsonConvert.DeserializeObject<T>(jsonMsg.Data),
                DeserializeContext(jsonMsg.Context));
        }

        private QueuePayload<T> DeserializeBinaryPayload<T>(byte[] data) where T : class, IMessage, new()
        {
            var binaryMsg = new QueueBinaryMsg();
            binaryMsg.MergeFrom(data);
            return new QueuePayload<T>(binaryMsg.Data.Unpack<T>(),
                DeserializeContext(binaryMsg.Context));
        }

        protected override Task DoUnsubscribeAsync<T>()
        {
            var queueName = GetQueueName<T>();
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_stanSubscriptions.TryRemove(queueName, out var stanSubscription))
            {
                stanSubscription.Unsubscribe();
            }

            return Task.CompletedTask;
        }


        protected override async Task DoStopAsync()
        {
            await base.DoStopAsync();
            if (_disposed)
            {
                return;
            }

            foreach (var stanSubscription in _stanSubscriptions.Values)
            {
                stanSubscription.Unsubscribe();
            }

            foreach (var natsSubscription in _natsSubscriptions.Values)
            {
                natsSubscription.Unsubscribe();
            }

            _connection?.Close();
            _connection?.Dispose();
            _natsConn?.Close();
            _natsConn?.Dispose();

            _disposed = true;
        }

        public override Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync()
        {
            HealthStatus status = HealthStatus.Healthy;
            string? errorMessage = null;
            if (_natsConn != null)
            {
                switch (_natsConn.State)
                {
                    case ConnState.DISCONNECTED:
                        status = HealthStatus.Unhealthy;
                        errorMessage = "Disconnected from nats";
                        break;
                    case ConnState.CONNECTED:
                        status = HealthStatus.Healthy;
                        break;
                    case ConnState.CLOSED:
                        status = HealthStatus.Unhealthy;
                        errorMessage = "Connection to nats is closed";
                        break;
                    case ConnState.RECONNECTING:
                        status = HealthStatus.Degraded;
                        errorMessage = "Reconnecting to nats";
                        break;
                    case ConnState.CONNECTING:
                        status = HealthStatus.Degraded;
                        errorMessage = "Connecting to nats";
                        break;
                    case ConnState.DRAINING_SUBS:
                        status = HealthStatus.Degraded;
                        errorMessage = "Draining subs";
                        break;
                    case ConnState.DRAINING_PUBS:
                        status = HealthStatus.Degraded;
                        errorMessage = "Draining pubs";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                if (IsStarted)
                {
                    status = HealthStatus.Unhealthy;
                    errorMessage = "Nats connection is null";
                }
            }

            return Task.FromResult((status, errorMessage));
        }

        private IStanConnection CreateConnection()
        {
            IConnection? natsConn = null;
            var clientId = $"{_config.ClientName}_{Guid.NewGuid()}";
            try
            {
                natsConn = GetNatsConnection(clientId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Nats connection error ({ExType}): {ErrorText}. Connection error: {ConnectionError} - {ConnectionInnerError}. Nats urls: {NatsUrls}. Nats timeout: {NatsTimeout}",
                    ex.GetType(), ex.ToString(), natsConn?.LastError.ToString(),
                    natsConn?.LastError?.InnerException?.ToString(), _config.Servers, _config.ConnectionTimeout);
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
                options.ConnectTimeout = (int)_config.ConnectionTimeout.TotalMilliseconds;
                var cf = new StanConnectionFactory();
                var stanConnection = cf.CreateConnection(_config.ClusterName, clientId, options);
                if (stanConnection.NATSConnection.State == ConnState.CONNECTED)
                {
                    _natsConn = natsConn;
                    return stanConnection;
                }

                throw new Exception("nats conn is not connected");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Error while connecting to nats: {ErrorText}. Connection error: {ConnectionError} - {ConnectionInnerError}",
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
                        "NATS event error: {ErrorText}. Connection {Connection}. Subs: {Subscription}", args.Error,
                        args.Conn, args.Subscription);
                };
            opts.ClosedEventHandler =
                (sender, args) => { _logger.LogInformation("Stan connection closed: {Conn}", args.Conn); };
            opts.DisconnectedEventHandler =
                (sender, args) => { _logger.LogInformation("NATS connection disconnected: {Conn}", args.Conn); };
            opts.ReconnectedEventHandler =
                (sender, args) =>
                {
                    _logger.LogInformation("NATS connection reconnected: {Conn}", args.Conn);
                };
            if (_config.Servers.Any())
            {
                var servers = new List<string>();
                foreach (var server in _config.Servers)
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

                if (_config.Verbose)
                    _logger.LogInformation("Nats urls: {Urls}", servers);
                opts.Servers = servers.ToArray();
            }

            opts.Verbose = _config.Verbose;
            opts.Timeout = (int)_config.ConnectionTimeout.TotalMilliseconds;
            if (_config.Verbose)
                _logger.LogInformation("Nats timeout: {Timeout}", _config.ConnectionTimeout);

            return opts;
        }
    }
}
