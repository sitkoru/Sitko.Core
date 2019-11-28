using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.PersistentQueue.HostedService;
using Sitko.Core.PersistentQueue.Internal;

namespace Sitko.Core.PersistentQueue.Consumer
{
    public abstract class PersistentQueueConsumer<TMessage, TConnection> : PersistentQueueChannel<TConnection>,
        IPersistentQueueConsumer<TMessage>, IAsyncDisposable where TConnection : IPersistentQueueConnection
        where TMessage : IMessage, new()
    {
        private readonly ILogger<PersistentQueueConsumer<TMessage, TConnection>> _logger;
        private readonly PersistedQueueHostedServiceOptions<TMessage> _queueOptions;
        private Func<TMessage, PersistentQueueMessageContext, Task<bool>> _callback;
        private readonly ConcurrentDictionary<string, bool> _processingQueue = new ConcurrentDictionary<string, bool>();
        private readonly Channel<PersistentQueueMessage> _buffer = Channel.CreateBounded<PersistentQueueMessage>(2000);
        private readonly List<Task> _workers = new List<Task>();
        private readonly PersistentQueueMetricsCollector _metricsCollector;
        private bool _isDisposed;
        private string _queueName;

        public PersistentQueueConsumer(IPersistentQueueConnectionFactory<TConnection> connectionFactory,
            IOptions<PersistedQueueHostedServiceOptions<TMessage>> queueOptions,
            PersistentQueueMetricsCollector metricsCollector,
            ILogger<PersistentQueueConsumer<TMessage, TConnection>> logger) : base(
            connectionFactory)
        {
            _queueOptions = queueOptions.Value;
            _metricsCollector = metricsCollector;
            _logger = logger;
            _queueName = Activator.CreateInstance<TMessage>().GetQueueName();
            for (var i = 0; i < _queueOptions.Workers; i++)
            {
                _logger.LogDebug("Start worker ({type}) #{number}", typeof(TMessage), i + 1);
                _workers.Add(DoConsumeAsync());
            }
        }

        public async Task RunAsync(Func<TMessage, PersistentQueueMessageContext, Task<bool>> callback,
            PersistedQueueHostedServiceOptions<TMessage> options = null)
        {
            var connection = await _connectionFactory.GetConnection();
            _logger.LogInformation("Subscribe to {queueName}", _queueName);
            _callback = callback;
            await connection.SubscribeAsync(options ?? _queueOptions, _queueName, queueMessage =>
            {
                var message = _serializer.Deserialize(queueMessage.Data);
                var queueResult = _processingQueue.TryAdd(message.Id, true);
                if (queueResult)
                {
                    var buffered = _buffer.Writer.TryWrite(queueMessage);
                    if (!buffered)
                    {
                        _logger.LogError("Can't buffer message {type}", typeof(TMessage));
                    }
                }
                else
                {
                    _logger.LogWarning("Message {messageId} ({type}) already processing", message.Id, typeof(TMessage));
                }

                return Task.CompletedTask;
            });
        }

        public async Task RunWithResponseAsync<TResponse>(
            Func<TMessage, PersistentQueueMessageContext, Task<(bool isSuccess, TResponse response)>> callback,
            PersistedQueueHostedServiceOptions<TMessage> options = null)
            where TResponse : IMessage, new()
        {
            var connection = await _connectionFactory.GetConnection();
            await connection.SubscribeWithResponseAsync(options ?? _queueOptions, _queueName,
                async queueMessage =>
                {
                    var message = _serializer.Deserialize(queueMessage.Data);

                    var protoMessage = message.GetMessage<TMessage>();
                    if (protoMessage != null)
                    {
                        _logger.LogDebug("New proto message {messageId} {protoMessage} ({type})", message.Id,
                            protoMessage,
                            protoMessage.GetType());
                        try
                        {
                            var (isSuccess, response) = await callback(protoMessage, message.GetContext());
                            if (isSuccess)
                            {
                                _logger.LogDebug("Messages {messageId} processed", message.Id);

                                if (!string.IsNullOrEmpty(queueMessage.ReplyTo) && response != null)
                                {
                                    var replayMessage = _serializer.Create(response, message.GetContext());
                                    var payload = _serializer.Serialize(replayMessage);

                                    connection.Publish(queueMessage.ReplyTo, payload);
                                }
                            }

                            _logger.LogDebug("Messages {messageId} done", message.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception while processing message {type} {messageId}",
                                typeof(TMessage),
                                message.Id);
                        }
                    }
                    else
                    {
                        _logger.LogError("Bad message received");
                    }
                });
        }

        private async Task DoConsumeAsync()
        {
            while (await _buffer.Reader.WaitToReadAsync())
            {
                while (_buffer.Reader.TryRead(out var queueMessage))
                {
                    try
                    {
                        var start = Stopwatch.StartNew();
                        var message = _serializer.Deserialize(queueMessage.Data);

                        var protoMessage = message.GetMessage<TMessage>();
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
                                    await queueMessage.Ack();
                                    _logger.LogDebug("Messages {messageId} acked", message.Id);
                                }

                                start.Stop();
                                _metricsCollector.TrackProcess(protoMessage, start.ElapsedMilliseconds);
                                _logger.LogDebug("Messages {messageId} done", message.Id);
                            }
                            catch (Exception ex)
                            {
                                start.Stop();
                                _logger.LogError(ex, "Exception while processing message {type} {messageId}",
                                    typeof(TMessage),
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
                        _logger.LogCritical(ex, "Exception while processing message {type}. Msg: {message}",
                            typeof(TMessage),
                            ex.ToString());
                    }
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            
            var connection = await _connectionFactory.GetConnection();
            await connection.UnSubscribeAsync(_queueName);
            _buffer.Writer.TryComplete();
            await Task.WhenAll(_workers);
            _isDisposed = true;
        }
    }
}
