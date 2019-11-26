using System;
using System.Threading.Tasks;
using Sitko.Core.App;
using Sitko.Core.Metrics;
using Sitko.Core.PersistentQueue.InMemory;
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
            var consumer = GetScope().Get<IPersistentQueueConsumer<QueueMsg>>();
            var producer = GetScope().Get<IPersistentQueueProducer<QueueMsg>>();

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
        protected override Application ConfigureApplication(Application application, string name)
        {
            base.ConfigureApplication(application, name);
            application.AddModule<MetricsModule>();
            application.AddModule<InMemoryPersistentQueueModule<RequestResponseTest>, InMemoryPersistentQueueModuleOptions>((
                configuration, environment) => new InMemoryPersistentQueueModuleOptions());

            return application;
        }
    }
}
