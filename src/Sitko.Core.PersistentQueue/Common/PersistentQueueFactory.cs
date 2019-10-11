using Microsoft.Extensions.Logging;

namespace Sitko.Core.PersistentQueue.Common
{
    public abstract class PersistentQueueFactory
    {
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly PersistentQueueMetricsCollector MetricsCollector;
        protected readonly IPersistentQueueConnectionFactory ConnectionFactory;
        protected readonly string ConsumerGroupName;

        public PersistentQueueFactory(PersistentQueueOptions options, ILoggerFactory loggerFactory,
            PersistentQueueMetricsCollector metricsCollector,
            IPersistentQueueConnectionFactory connectionFactory)
        {
            LoggerFactory = loggerFactory;
            MetricsCollector = metricsCollector;
            ConnectionFactory = connectionFactory;
            ConsumerGroupName = options.ConsumerGroupName;
        }
    }
}
