using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Sitko.Core.PersistentQueue.Common;
using Sitko.Core.PersistentQueue.HostedService;
using Sitko.Core.PersistentQueue.Internal;
using STAN.Client;

namespace Sitko.Core.PersistentQueue.Consumer
{
    [SuppressMessage("ReSharper", "IDISP002")]
    public class PersistentQueueConsumer<T> : PersistentQueueChannel, IDisposable where T : IMessage, new()
    {
        private readonly string _queueGroupName;
        private readonly ILogger<PersistentQueueConsumer<T>> _logger;
        private readonly PersistentQueueMetricsCollector _metricsCollector;
        private readonly PersistedQueueHostedServiceOptions<T> _queueOptions;
        private readonly PersistentQueueMessageSerializer _serializer;
        private Func<T, PersistentQueueMessageContext, Task<bool>> _callback;
        private PersistentQueueConnector _connector;
        private readonly List<Task> _workers = new List<Task>();

        private readonly Channel<StanMsg> _buffer =
            Channel.CreateBounded<StanMsg>(2000);

        internal PersistentQueueConsumer(IPersistentQueueConnectionFactory connectionFactory,
            string queueGroupName,
            ILogger<PersistentQueueConsumer<T>> logger,
            PersistentQueueMetricsCollector metricsCollector, PersistedQueueHostedServiceOptions<T> queueOptions) : base(
            connectionFactory)
        {
            _queueGroupName = queueGroupName;
            _logger = logger;
            _metricsCollector = metricsCollector;
            _queueOptions = queueOptions;
            _serializer = new PersistentQueueMessageSerializer();
            for (var i = 0; i < _queueOptions.Workers; i++)
            {
                _logger.LogDebug("Start worker ({type}) #{number}", typeof(T), i + 1);
                _workers.Add(DoConsumeAsync());
            }
        }

        [SuppressMessage("ReSharper", "IDISP004")]
        [SuppressMessage("ReSharper", "AvoidAsyncVoid")]
        public async Task RunWithResponseAsync<TResponse>(
            Func<T, PersistentQueueMessageContext, Task<(bool isSuccess, TResponse response)>> callback)
            where TResponse : IMessage, new()
        {
            var empty = Activator.CreateInstance<T>();
            var connector = await GetConnectorAsync<T>();
            connector.Connection.NATSConnection.SubscribeAsync(empty.GetQueueName(), async (sender, args) =>
            {
                var message = _serializer.Deserialize(args.Message.Data);

                var protoMessage = message.GetMessage<T>();
                if (protoMessage != null)
                {
                    _logger.LogDebug(
                        "New proto message {messageId} {protoMessage} ({type})", message.Id, protoMessage,
                        protoMessage.GetType());
                    try
                    {
                        var (isSuccess, response) = await callback(protoMessage, message.GetContext());
                        if (isSuccess)
                        {
                            _logger.LogDebug("Messages {messageId} processed", message.Id);

                            if (!string.IsNullOrEmpty(args.Message.Reply) && response != null)
                            {
                                var queueMessage = _serializer.Create(response, message.GetContext());
                                var payload = _serializer.Serialize(queueMessage);

                                connector.Connection.NATSConnection.Publish(args.Message.Reply, payload);
                            }
                        }


                        _logger.LogDebug("Messages {messageId} done", message.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception while processing message {type} {messageId}", typeof(T),
                            message.Id);
                    }
                }
                else
                {
                    _logger.LogError("Bad message received");
                }
            });
        }

        public async Task RunAsync(Func<T, PersistentQueueMessageContext, Task<bool>> callback)
        {
            var options = StanSubscriptionOptions.GetDefaultOptions();
            if (_queueOptions.All)
            {
                options.DeliverAllAvailable();
                _queueOptions.Durable = false;
            }
            else
            {
                if (_queueOptions.StartAt.HasValue)
                {
                    options.StartAt(_queueOptions.StartAt.Value);
                    _queueOptions.Durable = false;
                }
            }

            if (_queueOptions.ManualAck)
            {
                options.AckWait = _queueOptions.AckWait;
                options.ManualAcks = true;
            }

            if (_queueOptions.MaxInFlight > 0)
            {
                options.MaxInflight = _queueOptions.MaxInFlight;
            }

            _callback = callback;
            var emptyMessage = Activator.CreateInstance<T>();
            _logger.LogInformation("Subscribe to {queueName}", emptyMessage.GetQueueName());
            _connector = await GetConnectorAsync<T>();
            _connector.Reconnected += (sender, args) => { Subscribe(options, emptyMessage); };
            Subscribe(options, emptyMessage);
        }

        private async Task DoConsumeAsync()
        {
            while (await _buffer.Reader.WaitToReadAsync())
            {
                while (_buffer.Reader.TryRead(out var stanMsg))
                {
                    try
                    {
                        var start = Stopwatch.StartNew();
                        var message = _serializer.Deserialize(stanMsg.Data);

                        var protoMessage = message.GetMessage<T>();
                        if (protoMessage != null)
                        {
                            _metricsCollector.TrackReceive(protoMessage, message.GetContext());
                            _logger.LogDebug(
                                "New proto message {messageId} {protoMessage} ({type})", message.Id, protoMessage,
                                protoMessage.GetType());
                            try
                            {
                                var result = await _callback(protoMessage, message.GetContext());
                                if (result)
                                {
                                    _logger.LogDebug("Messages {messageId} processed", message.Id);
                                }

                                if (_queueOptions.ManualAck)
                                {
                                    stanMsg.Ack();
                                    _logger.LogDebug("Messages {messageId} acked", message.Id);
                                }

                                start.Stop();
                                _metricsCollector.TrackProcess(protoMessage, start.ElapsedMilliseconds);
                                _logger.LogDebug("Messages {messageId} done", message.Id);
                            }
                            catch (Exception ex)
                            {
                                start.Stop();
                                _logger.LogError(ex, "Exception while processing message {type} {messageId}", typeof(T),
                                    message.Id);
                            }
                        }
                        else
                        {
                            _logger.LogError("Bad message received");
                        }

                        if (!_processingQueue.TryRemove(message.Id, out _))
                        {
                            _logger.LogError("Can't remove message {messageId} from processing queue", message.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Exception while processing message {type}. Msg: {message}", typeof(T),
                            ex.ToString());
                    }
                }
            }
        }

        [SuppressMessage("ReSharper", "IDISP003")]
        private void Subscribe(StanSubscriptionOptions options, T emptyMessage)
        {
            if (_queueOptions.Durable && !string.IsNullOrEmpty(_queueGroupName))
            {
                options.DurableName = _queueGroupName;
                _logger.LogInformation("Durable group name {durableName}", options.DurableName);

                _sub = _connector.Connection.Subscribe(emptyMessage.GetQueueName(), _queueGroupName, options,
                    (sender, args) => Handle(args.Message));
            }
            else
            {
                _sub = _connector.Connection.Subscribe(emptyMessage.GetQueueName(), options,
                    (sender, args) => Handle(args.Message));
            }
        }

        private readonly ConcurrentDictionary<string, bool> _processingQueue = new ConcurrentDictionary<string, bool>();
        private IStanSubscription _sub;

        private void Handle(StanMsg stanMsg)
        {
            var message = _serializer.Deserialize(stanMsg.Data);
            var queueResult = _processingQueue.TryAdd(message.Id, true);
            if (queueResult)
            {
                var buffered = _buffer.Writer.TryWrite(stanMsg);
                if (!buffered)
                {
                    _logger.LogError("Can't buffer message {type}", typeof(T));
                }
            }
            else
            {
                _logger.LogWarning("Message {messageId} ({type}) already processing", message.Id, typeof(T));
            }
        }

        public async Task StopAsync()
        {
            _sub?.Close();
            _buffer.Writer.TryComplete();
            await Task.WhenAll(_workers);
        }


        public void Dispose()
        {
            _connector.Dispose();
        }
    }
}
