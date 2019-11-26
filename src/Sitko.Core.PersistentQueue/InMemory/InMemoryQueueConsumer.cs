using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.PersistentQueue.Consumer;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue.InMemory
{
    public class InMemoryQueueConsumer<TMessage> : PersistentQueueConsumer<TMessage, InMemoryQueueConnection>
        where TMessage : IMessage, new()
    {
        public InMemoryQueueConsumer(IPersistentQueueConnectionFactory<InMemoryQueueConnection> connectionFactory,
            IOptions<PersistedQueueHostedServiceOptions<TMessage>> queueOptions,
            PersistentQueueMetricsCollector metricsCollector,
            ILogger<InMemoryQueueConsumer<TMessage>> logger) : base(connectionFactory,
            queueOptions, metricsCollector, logger)
        {
        }
    }
}
