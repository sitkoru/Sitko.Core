using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Sitko.Core.PersistentQueue.Internal;
using Sitko.Core.PersistentQueue.Queue;

namespace Sitko.Core.PersistentQueue.Producer
{
    public abstract class PersistentQueueProducer<T, TConnection> : PersistentQueueChannel<TConnection>,
        IPersistentQueueProducer<T>
        where T : IMessage where TConnection : IPersistentQueueConnection
    {
        private readonly ILogger<PersistentQueueProducer<T, TConnection>> _logger;
        private readonly PersistentQueueMetricsCollector _metricsCollector;
        private readonly Task _sendTask;

        private readonly Channel<(T message, PersistentQueueMessageContext context)> _channel =
            Channel.CreateUnbounded<(T message, PersistentQueueMessageContext context)>();

        public PersistentQueueProducer(IPersistentQueueConnectionFactory<TConnection> connectionFactory,
            ILogger<PersistentQueueProducer<T, TConnection>> logger,
            PersistentQueueMetricsCollector metricsCollector) : base(connectionFactory)
        {
            _logger = logger;
            _metricsCollector = metricsCollector;
            _sendTask = DoProduceAsync();
        }

        private async Task DoProduceAsync()
        {
            _logger.LogInformation("Start background publisher for {type} messages", typeof(T));
            while (await _channel.Reader.WaitToReadAsync())
            {
                while (_channel.Reader.TryRead(out var sendItem))
                {
                    var (message, context) = sendItem;
                    try
                    {
                        await ProduceAsync(message, context);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, e.ToString());
                        _channel.Writer.TryWrite((message, context));
                    }
                }
            }

            _logger.LogInformation("Stop background publisher for {type} messages", typeof(T));
        }

        private async Task ProduceAsync(T message, PersistentQueueMessageContext context = null)
        {
            var queueMessage = GetMessage(message, context);
            var queue = message.GetQueueName();
            var payload = SerializeMessage(queueMessage);
            try
            {
                var connection = await _connectionFactory.GetConnection();
                await connection.PublishAsync(queue, payload);
                _logger.LogDebug("Message {messageId} was sent to {queueName}", queueMessage.Id,
                    message.GetQueueName());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Fail send message {messageId} to {queueName}: {error}", queueMessage.Id, message.GetQueueName(),
                    ex.ToString());
            }
        }

        public async Task<(TResponse response, PersistentQueueMessageContext responseContext)> RequestAsync<TResponse>(
            T message,
            PersistentQueueMessageContext context = null, int offset = 5000) where TResponse : class, IMessage, new()
        {
            var queueMessage = GetMessage(message, context);
            var queue = message.GetQueueName();
            var payload = SerializeMessage(queueMessage);
            try
            {
                var connection = await _connectionFactory.GetConnection();
                var result = await connection.RequestAsync(queue, payload, offset);
                var stanMsg = _serializer.Deserialize(result);
                var response = stanMsg.GetMessage<TResponse>();
                _logger.LogDebug("Message {messageId} was sent to {queueName}", queueMessage.Id,
                    message.GetQueueName());

                return (response, stanMsg.GetContext());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Fail send message {messageId} to {queueName}: {error}", queueMessage.Id, message.GetQueueName(),
                    ex.ToString());
                throw;
            }
        }

        private QueueMsg GetMessage(IMessage message, PersistentQueueMessageContext context = null)
        {
            _metricsCollector.TrackSend(message);
            var queueMessage = _serializer.Create(message, context);
            _logger.LogDebug("Send message {messageId} to {queueName}", queueMessage.Id, message.GetQueueName());
            return queueMessage;
        }

        private byte[] SerializeMessage(QueueMsg queueMsg)
        {
            var payload = _serializer.Serialize(queueMsg);
            _metricsCollector.TrackSize(queueMsg, payload.LongLength);
            return payload;
        }

        public void Produce(T message, PersistentQueueMessageContext context = null)
        {
            var sw = Stopwatch.StartNew();
            var success = _channel.Writer.TryWrite((message, context));
            sw.Stop();
            _metricsCollector.TrackProduce(message, sw.ElapsedMilliseconds, success);
            if (!success)
            {
                _logger.LogError("Error while write message {type} to buffer", typeof(T));
            }
        }

        public override void Dispose()
        {
            _channel.Writer.TryComplete();
            _sendTask.GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}
