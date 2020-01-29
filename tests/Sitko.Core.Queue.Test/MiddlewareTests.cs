using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Metrics;
using Sitko.Core.Queue.Middleware;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Tests
{
    public class MiddlewareTests : BaseTest
    {
        public MiddlewareTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Publish()
        {
            var scope = GetScope<MiddlewareQueueTestScope>();

            var queue = scope.Get<IQueue>();

            var mw = scope.Get<CountMiddleware>();

            Assert.Equal(0, mw.Published);

            var publishResult = await queue.PublishAsync(new TestMessage());
            Assert.True(publishResult.IsSuccess);

            Assert.Equal(1, mw.Published);
        }

        [Fact]
        public async Task Receive()
        {
            var scope = GetScope<MiddlewareQueueTestScope>();

            var queue = scope.Get<IQueue>();

            var mw = scope.Get<CountMiddleware>();

            Assert.Equal(0, mw.Received);

            var publishResult = await queue.PublishAsync(new TestMessage());
            Assert.True(publishResult.IsSuccess);

            Assert.Equal(0, mw.Received);

            var subResult = await queue.SubscribeAsync<TestMessage>((message, context) => Task.FromResult(true));
            Assert.True(subResult.IsSuccess);

            publishResult = await queue.PublishAsync(new TestMessage());
            Assert.True(publishResult.IsSuccess);

            Assert.Equal(1, mw.Received);
        }

        [Fact]
        public void Multiple()
        {
            var scope = GetScope<MultipleMiddlewareQueueTestScope>();

            var mws = scope.GetAll<IQueueMiddleware>();

            Assert.Equal(2, mws.Count());
        }

        [Fact]
        public async Task Metrics()
        {
            var scope = GetScope<MetricsMiddlewareQueueTestScope>();

            var mw = scope.Get<MetricsMiddleware>();

            Assert.NotNull(mw);
            Assert.Equal(0, mw.SentCount);
            Assert.Equal(0, mw.ReceivedCount);
            Assert.Equal(0, mw.AvgProcessTime);
            Assert.Equal(0, mw.AvgLatency);

            var queue = scope.Get<IQueue>();
            var subResult = await queue.SubscribeAsync<TestMessage>(async (message, context) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return true;
            });
            Assert.True(subResult.IsSuccess);

            var msg = new TestMessage();
            var result = await queue.PublishAsync(msg,
                new QueueMessageContext
                {
                    RootMessageId = Guid.NewGuid(), RootMessageDate = DateTimeOffset.UtcNow.AddMinutes(-1)
                });
            Assert.True(result.IsSuccess);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(1, mw.SentCount);
            Assert.Equal(1, mw.ReceivedCount);
            Assert.NotEqual(0, mw.AvgProcessTime);
            Assert.NotEqual(0, mw.AvgLatency);
        }
    }

    public class MiddlewareQueueTestScope : BaseTestQueueTestScope
    {
        protected override void ConfigureQueue(TestQueueConfig config, IConfiguration configuration,
            IHostEnvironment environment, string name)
        {
            base.ConfigureQueue(config, configuration, environment, name);
            config.RegisterMiddleware<CountMiddleware>();
        }
    }

    public class MetricsMiddlewareQueueTestScope : BaseTestQueueTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name).AddModule<MetricsModule>();
        }

        protected override void ConfigureQueue(TestQueueConfig config, IConfiguration configuration,
            IHostEnvironment environment, string name)
        {
            base.ConfigureQueue(config, configuration, environment, name);
            config.EnableMetrics();
        }
    }

    public class MultipleMiddlewareQueueTestScope : BaseTestQueueTestScope
    {
        protected override void ConfigureQueue(TestQueueConfig config, IConfiguration configuration,
            IHostEnvironment environment, string name)
        {
            base.ConfigureQueue(config, configuration, environment, name);
            config.RegisterMiddlewares<MiddlewareTests>();
        }
    }

    public class CountMiddleware : IQueueMiddleware
    {
        public int Published { get; private set; }
        public int Received { get; private set; }

        public Task OnAfterPublishAsync(object message, QueueMessageContext messageContext)
        {
            Published++;
            return Task.CompletedTask;
        }

        public Task OnAfterReceiveAsync(object message, QueueMessageContext messageContext)
        {
            Received++;
            return Task.CompletedTask;
        }
    }
}
