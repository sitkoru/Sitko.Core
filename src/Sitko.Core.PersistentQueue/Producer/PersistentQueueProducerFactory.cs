using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Sitko.Core.PersistentQueue.Common;

namespace Sitko.Core.PersistentQueue.Producer
{
    public class PersistentQueueProducerFactory : PersistentQueueFactory
    {
        public PersistentQueueProducerFactory(PersistentQueueOptions options, ILoggerFactory loggerFactory,
            PersistentQueueMetricsCollector metricsCollector, IPersistentQueueConnectionFactory connectionFactory)
            : base(options, loggerFactory, metricsCollector, connectionFactory)
        {
        }

        public PersistentQueueProducer<T> GetProducer<T>() where T : IMessage
        {
            return new PersistentQueueProducer<T>(ConnectionFactory,
                LoggerFactory.CreateLogger<PersistentQueueProducer<T>>(),
                MetricsCollector);
        }
    }
}
