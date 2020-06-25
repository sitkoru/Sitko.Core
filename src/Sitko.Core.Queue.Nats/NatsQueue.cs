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

        private readonly ConcurrentDictionary<string, Action> _subscribeActions =
            new ConcurrentDictionary<string, Action>();

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
            EnsureConnected();
            return Task.CompletedTask;
        }

        private string GetQueueName<T>(T message) where T : class
        {
            string queueName = message.GetType().FullName;
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

        private IStanConnection GetConnection()
        {
            if (_connection == null)
            {
                throw new Exception("Stan connection is not established");
            }

            if (_connection.NATSConnection == null)
            {
                throw new Exception("Nats connection is null");
            }

            if (_connection.NATSConnection.State != ConnState.CONNECTED)
            {
                throw new Exception("Stan connection is not connected");
            }

            return _connection;
        }

        protected override async Task<QueuePublishResult> DoPublishAsync<T>(T message, QueueMessageContext context)
        {
            var result = new QueuePublishResult();
            try
            {
                await GetConnection().PublishAsync(GetQueueName(message), SerializePayload(message, context));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error publishing message {MessageType} to Nats: {ErrorText}", message.GetType(),
                    e.ToString());
                result.SetException(e);
            }

            return result;
        }

        protected override async Task<(TResponse message, QueueMessageContext context)?> DoRequestAsync<TMessage,
            TResponse>(
            TMessage message, QueueMessageContext context, TimeSpan timeout)
        {
            if (_natsConn == null)
            {
                throw new Exception("Connection to nats not etablished");
            }

            try
            {
                var deserializer = GetPayloadDeserializer<TResponse>();
                var result =
                    await _natsConn.RequestAsync(GetQueueName(message), SerializePayload(message, context),
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
            Func<TMessage, QueueMessageContext, PublishAsyncDelegate<TResponse>, Task<bool>> callback)
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
                    await callback(request.message, request.messageContext, (message, context) =>
                    {
                        var data = SerializePayload(message, context);
                        var result = new QueuePublishResult();
                        try
                        {
                            args.Message.Respond(data);
                        }
                        catch (NATSException ex)
                        {
                            _logger.LogError(ex, "Error responding to message {MessageId} (MessageType): {ErrorText}",
                                request.messageContext.Id, request.messageContext.MessageType, ex.ToString());
                            result.SetException(ex);
                        }

                        return Task.FromResult(result);
                    });
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

        private byte[] SerializePayload<T>(T message, QueueMessageContext context) where T : class
        {
            var contextMsg = new QueueContextMsg
            {
                Id = context.Id.ToString(),
                Date = context.Date.ToTimestamp(),
                MessageType = context.MessageType ?? string.Empty
            };
            if (!string.IsNullOrEmpty(context.RequestId))
            {
                contextMsg.RequestId = context.RequestId;
            }

            if (context.ParentMessageId != null)
            {
                contextMsg.ParentMessageId = context.ParentMessageId.ToString();
            }

            if (context.RootMessageId != null)
            {
                contextMsg.RootMessageId = context.RootMessageId.ToString();
            }

            if (context.RootMessageDate != null)
            {
                contextMsg.RootMessageDate = context.RootMessageDate.Value.ToTimestamp();
            }

            IMessage msg;
            if (message is IMessage protoMessage)
            {
                msg = new QueueBinaryMsg {Context = contextMsg, Data = Any.Pack(protoMessage)};
            }
            else
            {
                msg = new QueueJsonMsg {Context = contextMsg, Data = JsonConvert.SerializeObject(message)};
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
            var result = new QueueSubscribeResult();
            var queueName = GetQueueName<T>();
            if (!_stanSubscriptions.ContainsKey(queueName))
            {
                DoSubscribe(queueName, options);
                _subscribeActions.TryAdd(queueName, () => DoSubscribe(queueName, options));
            }

            return Task.FromResult(result);
        }

        private void DoSubscribe<T>(string queueName, IQueueMessageOptions<T>? options = null) where T : class
        {
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
                sub = GetConnection().Subscribe(queueName,
                    _config.ConsumerGroupName, stanOptions,
                    async (sender, args) =>
                        await ProcessStanMessage(deserializer, args.Message, stanOptions.ManualAcks));
            }
            else
            {
                sub = GetConnection().Subscribe(queueName, stanOptions,
                    async (sender, args) =>
                        await ProcessStanMessage(deserializer, args.Message, stanOptions.ManualAcks));
            }

            _logger.LogInformation("{QueueName}: Subscribed", queueName);

            _stanSubscriptions.TryAdd(queueName, sub);
        }

        private async Task ProcessStanMessage<T>(
            Func<byte[], (T message, QueueMessageContext messageContext)> deserializer, StanMsg message,
            bool manualAcks) where T : class
        {
            try
            {
                var payload = deserializer(message.Data);
                var processResult = await ProcessMessageAsync(payload.message, payload.messageContext);
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

        private Func<byte[], (T message, QueueMessageContext messageContext)> GetPayloadDeserializer<T>()
            where T : class
        {
            var isBinary = typeof(IMessage).IsAssignableFrom(typeof(T));
            Func<byte[], (T message, QueueMessageContext messageContext)> deserializer;
            if (isBinary)
            {
                var processMethod = _deserializeBinaryMethod.MakeGenericMethod(typeof(T));
                if (processMethod == null)
                {
                    throw new Exception($"Can't create generic deserialize method for {typeof(T)}");
                }

                deserializer = bytes =>
                    ((T message, QueueMessageContext messageContext))processMethod.Invoke(this, new object[] {bytes});
            }
            else
            {
                deserializer = DeserializeJsonPayload<T>;
            }

            return deserializer;
        }

        private (T message, QueueMessageContext messageContext) DeserializeJsonPayload<T>(byte[] data) where T : class
        {
            var jsonMsg = new QueueJsonMsg();
            jsonMsg.MergeFrom(data);
            return (JsonConvert.DeserializeObject<T>(jsonMsg.Data), DeserializeContext(jsonMsg.Context));
        }

        private (T message, QueueMessageContext messageContext) DeserializeBinaryPayload<T>(byte[] data)
            where T : class, IMessage, new()
        {
            var binaryMsg = new QueueBinaryMsg();
            binaryMsg.MergeFrom(data);
            return (binaryMsg.Data.Unpack<T>(), DeserializeContext(binaryMsg.Context));
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
                _subscribeActions.Remove(queueName, out _);
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

        private IConnection CreateNatsConnection(string clientId)
        {
            IConnection? natsConn = null;
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

            return natsConn;
        }

        private IStanConnection CreateConnection(string clientId, IConnection natsConn)
        {
            if (natsConn.State != ConnState.CONNECTED)
                throw new Exception("nats conn is not connected");
            try
            {
                var options = StanOptions.GetDefaultOptions();
                options.NatsConn = natsConn;
                options.ConnectTimeout = (int)_config.ConnectionTimeout.TotalMilliseconds;
                options.ConnectionLostEventHandler += (sender, args) =>
                {
                    _logger.LogError("Stan connection is broken");
                    EnsureConnected();
                };
                var cf = new StanConnectionFactory();

                var stanConnection = cf.CreateConnection(_config.ClusterName, clientId, options);
                if (stanConnection.NATSConnection.State == ConnState.CONNECTED)
                {
                    _natsConn = natsConn;
                    foreach (var action in _subscribeActions.Values)
                    {
                        action();
                    }

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

        private void EnsureConnected()
        {
            var clientId = $"{_config.ClientName}_{Guid.NewGuid()}";
            if (_natsConn != null)
            {
                clientId = _natsConn.Opts.Name;
            }
            else
            {
                _natsConn = CreateNatsConnection(clientId);
            }

            if (_connection != null)
            {
                try
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.ToString());
                }
            }

            _connection = null;
            _connection = CreateConnection(clientId, _natsConn);
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
                    EnsureConnected();
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
