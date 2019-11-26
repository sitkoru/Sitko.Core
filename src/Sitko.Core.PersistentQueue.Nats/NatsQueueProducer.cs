using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Sitko.Core.PersistentQueue.Producer;

namespace Sitko.Core.PersistentQueue.Nats
{
    public class NatsQueueProducer<TMessage> : PersistentQueueProducer<TMessage, NatsQueueConnection>
        where TMessage : IMessage
    {
        public NatsQueueProducer(IPersistentQueueConnectionFactory<NatsQueueConnection> connectionFactory,
            ILogger<NatsQueueProducer<TMessage>> logger,
            PersistentQueueMetricsCollector metricsCollector) : base(connectionFactory, logger, metricsCollector)
        {
        }
    }
}
