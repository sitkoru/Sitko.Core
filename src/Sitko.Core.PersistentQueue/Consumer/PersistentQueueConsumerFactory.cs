using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Sitko.Core.PersistentQueue.Common;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue.Consumer
{
    public class PersistentQueueConsumerFactory : PersistentQueueFactory
    {
        public PersistentQueueConsumerFactory(PersistentQueueOptions options, ILoggerFactory loggerFactory,
            PersistentQueueMetricsCollector metricsCollector, IPersistentQueueConnectionFactory connectionFactory)
            : base(options, loggerFactory, metricsCollector, connectionFactory)
        {
        }

        public PersistentQueueConsumer<T> GetConsumer<T>(PersistedQueueHostedServiceOptions<T> options)
            where T : IMessage, new()
        {
            var consumer = new PersistentQueueConsumer<T>(ConnectionFactory, ConsumerGroupName,
                LoggerFactory.CreateLogger<PersistentQueueConsumer<T>>(), MetricsCollector, options);

            return consumer;
        }
    }
}
