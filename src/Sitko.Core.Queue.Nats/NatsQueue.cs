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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client;
using Newtonsoft.Json;
using Sitko.Core.Queue.Exceptions;
using Sitko.Core.Queue.Internal;
using STAN.Client;
using Options = NATS.Client.Options;

namespace Sitko.Core.Queue.Nats
{
    public class NatsQueue : BaseQueue<NatsQueueModuleConfig>
    {
        private IStanConnection? _connection;
        private static readonly ConnectionFactory _connectionFactory = new();

        private readonly ConcurrentDictionary<Guid, IAsyncSubscription> _natsSubscriptions = new();

        private readonly ConcurrentDictionary<string, Action> _subscribeActions = new();

        private readonly MethodInfo _deserializeBinaryMethod =
            typeof(NatsQueue).GetMethod(nameof(DeserializeBinaryPayload),
                BindingFlags.NonPublic | BindingFlags.Instance)!;

        private readonly ConcurrentDictionary<string, IStanSubscription> _stanSubscriptions = new();

        private bool _disposed;
        private IConnection? _natsConn;
        private readonly string _consumerGroupName;
        private readonly string _clientName;


        public NatsQueue(IOptionsMonitor<NatsQueueModuleConfig> config, QueueContext context,
            IHostEnvironment environment,
            ILogger<NatsQueue> logger) : base(config,
            context, logger)
        {
            _clientName = environment.ApplicationName.Replace('.', '_');
            _consumerGroupName = !string.IsNullOrEmpty(Config.ConsumerGroupName)
                ? Config.ConsumerGroupName
                : environment.ApplicationName;
        }

        protected override Task DoStartAsync()
        {
            GetConnection();
            return Task.CompletedTask;
        }

        private string GetQueueName<T>(T message) where T : class
        {
            string queueName = message.GetType().FullName;
            if (message is IMessage protoMessage)
            {
                queueName = protoMessage.Descriptor.FullName;
            }

            if (!string.IsNullOrEmpty(Config.QueueNamePrefix))
            {
                queueName = $"{Config.QueueNamePrefix}_{queueName}";
            }

            return queueName;
        }

        private string GetQueueName<T>() where T : class
        {
            return GetQueueName(Activator.CreateInstance<T>());
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
                Logger.LogError(e, "Error publishing message {MessageType} to Nats: {ErrorText}", message.GetType(),
                    e.ToString());
                result.SetException(e);
            }

            return result;
        }

        protected override async Task<(TResponse message, QueueMessageContext context)?> DoRequestAsync<TMessage,
            TResponse>(
            TMessage message, QueueMessageContext context, TimeSpan timeout)
        {
            try
            {
                var clientId = $"nats_request_{Guid.NewGuid()}";
                using var conn = CreateNatsConnection(clientId);
                var deserializer = GetPayloadDeserializer<TResponse>();
                var result =
                    await conn.RequestAsync(GetQueueName(message), SerializePayload(message, context),
                        (int)timeout.TotalMilliseconds);
                try
                {
                    return deserializer(result.Data);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while deserializing response of type {Type}: {ErrorText}",
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
            var clientId = $"nats_request_{Guid.NewGuid()}";
            var conn = CreateNatsConnection(clientId);
            var deserializer = GetPayloadDeserializer<TMessage>();
            if (deserializer == null)
            {
                throw new Exception($"Empty deserialize method for {typeof(TMessage)}");
            }

            var queue = GetQueueName<TMessage>();
            var id = Guid.NewGuid();
            var sub = conn.SubscribeAsync(queue,
                // ReSharper disable once AsyncVoidLambda
                async (_, args) =>
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
                            Logger.LogError(ex, "Error responding to message {MessageId} {MessageType}: {ErrorText}",
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
                natsSubscription.Connection.Close();
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
                Logger.LogInformation("{QueueName}: Load all messages", queueName);
                stanOptions.DeliverAllAvailable();
                queueOptions.Durable = false;
            }
            else
            {
                if (queueOptions.StartAt.HasValue)
                {
                    Logger.LogInformation("{QueueName}: Load all messages starts from {Date}", queueName,
                        queueOptions.StartAt.Value);
                    stanOptions.StartAt(queueOptions.StartAt.Value);
                    queueOptions.Durable = false;
                }
            }

            if (queueOptions.ManualAck)
            {
                Logger.LogInformation("{QueueName}: Manual acks with {Timeout} timeout", queueName,
                    queueOptions.AckWait);
                stanOptions.AckWait = (int)queueOptions.AckWait.TotalMilliseconds;
                stanOptions.ManualAcks = true;
            }

            if (queueOptions.MaxInFlight > 0)
            {
                Logger.LogInformation("{QueueName}: {MaxInFlight} max in flight", queueName, queueOptions.MaxInFlight);
                stanOptions.MaxInflight = queueOptions.MaxInFlight;
            }

            if (queueOptions.Durable && !string.IsNullOrEmpty(_consumerGroupName))
            {
                Logger.LogInformation("{QueueName}: Durable name - {DurableName}", queueName,
                    _consumerGroupName);
                stanOptions.DurableName = _consumerGroupName;
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
                    _consumerGroupName, stanOptions,
                    // ReSharper disable once AsyncVoidLambda
                    async (_, args) =>
                        await ProcessStanMessage(deserializer, args.Message, stanOptions.ManualAcks));
            }
            else
            {
                sub = GetConnection().Subscribe(queueName, stanOptions,
                    // ReSharper disable once AsyncVoidLambda
                    async (_, args) =>
                        await ProcessStanMessage(deserializer, args.Message, stanOptions.ManualAcks));
            }

            Logger.LogInformation("{QueueName}: Subscribed", queueName);

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
                Logger.LogError(ex, "Error while processing message of type {Type}: {ErrorText}", typeof(T),
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
            return (JsonConvert.DeserializeObject<T>(jsonMsg.Data), DeserializeContext(jsonMsg.Context))!;
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

            _disposed = true;
        }

        public override Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync()
        {
            HealthStatus status = HealthStatus.Healthy;
            string? errorMessage = null;
            if (_connection != null)
            {
                switch (_connection.NATSConnection.State)
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
                Logger.LogCritical(ex,
                    "Nats connection error ({ExType}): {ErrorText}. Connection error: {ConnectionError} - {ConnectionInnerError}. Nats urls: {NatsUrls}. Nats timeout: {NatsTimeout}",
                    ex.GetType(), ex.ToString(), natsConn?.LastError.ToString(),
                    natsConn?.LastError?.InnerException?.ToString(), Config.Servers, Config.ConnectionTimeout);
                try
                {
                    natsConn?.Close();
                }
                catch (Exception connEx)
                {
                    Logger.LogError(connEx, "Error connecting to nats: {ErrorText}", connEx.ToString());
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
                options.ConnectTimeout = (int)Config.ConnectionTimeout.TotalMilliseconds;
                options.ConnectionLostEventHandler += (_, _) =>
                {
                    Logger.LogError("Stan connection is broken");
                    GetConnection();
                };
                var cf = new StanConnectionFactory();

                var stanConnection = cf.CreateConnection(Config.ClusterName, clientId, options);
                if (stanConnection.NATSConnection.State == ConnState.CONNECTED)
                {
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
                Logger.LogCritical(ex,
                    "Error while connecting to nats: {ErrorText}. Connection error: {ConnectionError} - {ConnectionInnerError}",
                    ex.ToString(), natsConn.LastError?.ToString(), natsConn.LastError?.InnerException?.ToString());
                throw;
            }
        }

        private IStanConnection GetConnection()
        {
            var isNew = false;
            var clientId = $"{_clientName}_{Guid.NewGuid()}";
            if (_connection == null || _connection.NATSConnection.State != ConnState.CONNECTED)
            {
                Logger.LogWarning("Nats connection is null or not connected");
                isNew = true;
            }

            if (isNew && _connection != null)
            {
                try
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error disposing nats connection: {ErrorText}", ex.ToString());
                }
                finally
                {
                    _connection = null;
                }
            }

            if (_connection == null)
            {
                var clientId = $"{_config.ClientName}_{Guid.NewGuid()}";
                Logger.LogWarning("Stan connection is not established");
                _connection = CreateConnection(clientId, CreateNatsConnection(clientId));
            }

            return _connection;
        }

        private IConnection GetNatsConnection(string clientId)
        {
            var opts = GetOptions();
            opts.Name = clientId;
            return _connectionFactory.CreateConnection(opts);
        }

        private Options GetOptions()
        {
            var opts = ConnectionFactory.GetDefaultOptions();
            opts.AllowReconnect = true;
            opts.MaxReconnect = Options.ReconnectForever;
            opts.PingInterval = 1000;
            opts.MaxPingsOut = 3;
            opts.AsyncErrorEventHandler =
                (_, args) =>
                {
                    Logger.LogError(
                        "NATS event error: {ErrorText}. Connection {Connection}. Subs: {Subscription}", args.Error,
                        args.Conn, args.Subscription);
                };
            opts.ClosedEventHandler =
                (_, args) => { Logger.LogInformation("Stan connection closed: {Conn}", args.Conn); };
            opts.DisconnectedEventHandler =
                (_, args) => { Logger.LogInformation("NATS connection disconnected: {Conn}", args.Conn); };
            opts.ReconnectedEventHandler =
                (_, args) =>
                {
                    Logger.LogInformation("NATS connection reconnected: {Conn}", args.Conn);
                    GetConnection();
                };
            if (Config.Servers.Any())
            {
                var servers = new List<string>();
                foreach (var server in Config.Servers)
                {
                    if (IPAddress.TryParse(server.Host, out var ip))
                    {
                        servers.Add($"nats://{ip}:{server.Port}");
                    }
                    else
                    {
                        var entry = Dns.GetHostEntry(server.Host);
                        if (entry.AddressList.Any())
                        {
                            foreach (var ipAddress in entry.AddressList)
                            {
                                servers.Add($"nats://{ipAddress}:{server.Port}");
                            }
                        }
                        else
                        {
                            throw new Exception($"Can't resolve ip for host {server.Port}");
                        }
                    }
                }

                if (Config.Verbose)
                    Logger.LogInformation("Nats urls: {Urls}", servers);
                opts.Servers = servers.ToArray();
            }

            opts.Verbose = Config.Verbose;
            opts.Timeout = (int)Config.ConnectionTimeout.TotalMilliseconds;
            if (Config.Verbose)
                Logger.LogInformation("Nats timeout: {Timeout}", Config.ConnectionTimeout);

            return opts;
        }
    }
}
