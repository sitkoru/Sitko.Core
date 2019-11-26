using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.PersistentQueue.Consumer;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue.Nats
{
    public class NatsQueueConsumer<TMessage> : PersistentQueueConsumer<TMessage, NatsQueueConnection>
        where TMessage : IMessage, new()
    {
        public NatsQueueConsumer(IPersistentQueueConnectionFactory<NatsQueueConnection> connectionFactory,
            IOptions<PersistedQueueHostedServiceOptions<TMessage>> queueOptions,
            PersistentQueueMetricsCollector metricsCollector,
            ILogger<NatsQueueConsumer<TMessage>> logger) : base(connectionFactory,
            queueOptions, metricsCollector, logger)
        {
        }
    }
}
