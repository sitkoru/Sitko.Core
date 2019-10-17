using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Metrics;
using Sitko.Core.PersistentQueue.Common;
using Sitko.Core.PersistentQueue.Consumer;
using Sitko.Core.PersistentQueue.HostedService;
using Sitko.Core.PersistentQueue.Internal;
using Sitko.Core.PersistentQueue.Producer;
using Sitko.Core.PersistentQueue.Queue;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.PersistentQueue.Tests
{
    public class RequestResponseTest : BaseTest<PersistentQueueTestScope>
    {
        [Fact]
        public async Task TestRequestResponseAsync()
        {
            var metricsCollector = new PersistentQueueMetricsCollector(new FakeMetricsCollector());
            var options = new PersistentQueueOptions
            {
                ClusterName = "cg2",
                Servers = new List<string> {"nats://localhost:4222"},
                ClientName = $"tests{Guid.NewGuid()}"
            };
            var loggerFactory = GetScope().Get<ILoggerFactory>();
            var connectionsFactory = new SinglePersistentQueueConnectionFactory(options,
                loggerFactory.CreateLogger<SinglePersistentQueueConnectionFactory>());
            var consumerFactory = new PersistentQueueConsumerFactory(options, loggerFactory, metricsCollector,
                connectionsFactory);
            var producerFactory =
                new PersistentQueueProducerFactory(options, loggerFactory, metricsCollector,
                    connectionsFactory);

            var consumer =
                consumerFactory.GetConsumer(
                    new PersistedQueueHostedServiceOptions<QueueMsg>());
            var producer = producerFactory.GetProducer<QueueMsg>();

            await consumer.RunWithResponseAsync((msg, context) =>
                Task.FromResult((true, new QueueMsg {Id = msg.Id})));

            var id = Guid.NewGuid().ToString();
            var request = new QueueMsg {Id = id};
            var result = await producer.RequestAsync<QueueMsg>(request);
            Assert.Equal(id, result.response.Id);
        }

        public RequestResponseTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }

    public class PersistentQueueTestScope : BaseTestScope
    {
    }
}
