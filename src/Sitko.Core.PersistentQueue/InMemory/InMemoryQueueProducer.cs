using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Sitko.Core.PersistentQueue.Producer;

namespace Sitko.Core.PersistentQueue.InMemory
{
    public class InMemoryQueueProducer<TMessage> : PersistentQueueProducer<TMessage, InMemoryQueueConnection>
        where TMessage : IMessage
    {
        public InMemoryQueueProducer(IPersistentQueueConnectionFactory<InMemoryQueueConnection> connectionFactory,
            ILogger<PersistentQueueProducer<TMessage, InMemoryQueueConnection>> logger,
            PersistentQueueMetricsCollector metricsCollector) : base(connectionFactory, logger, metricsCollector)
        {
        }
    }
}
