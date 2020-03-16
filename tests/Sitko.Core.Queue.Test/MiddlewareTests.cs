using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            var classes =
                typeof(MiddlewareTests).Assembly.ExportedTypes.Where(t => typeof(IQueueMiddleware).IsAssignableFrom(t));

            var mws = scope.GetAll<IQueueMiddleware>();

            Assert.Equal(classes.Count(), mws.Count());
        }

        [Fact]
        public async Task Chain()
        {
            var scope = GetScope<ChainMiddlewareQueueTestScope>();

            var state = scope.Get<ChainState>();

            Assert.Null(state.State);

            var queue = scope.Get<IQueue>();

            var publishResult = await queue.PublishAsync(new TestMessage());
            Assert.True(publishResult.IsSuccess);

            Assert.Equal("foobar", state.State);
        }
    }

    public class MiddlewareQueueTestScope : BaseTestQueueTestScope
    {
        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            TestQueueConfig config, string name)
        {
            config.RegisterMiddleware<CountMiddleware>();
        }
    }

    public class MultipleMiddlewareQueueTestScope : BaseTestQueueTestScope
    {
        protected override IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment,
            IServiceCollection services, string name)
        {
            return base.ConfigureServices(configuration, environment, services, name).AddSingleton<ChainState>();
        }

        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            TestQueueConfig config, string name)
        {
            config.RegisterMiddlewares<MiddlewareTests>();
        }
    }

    public class ChainMiddlewareQueueTestScope : BaseTestQueueTestScope
    {
        protected override IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment,
            IServiceCollection services, string name)
        {
            return base.ConfigureServices(configuration, environment, services, name).AddSingleton<ChainState>();
        }

        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            TestQueueConfig config, string name)
        {
            config.RegisterMiddleware<ChainFooMiddleware>();
            config.RegisterMiddleware<ChainBarMiddleware>();
        }
    }

    public class ChainState
    {
        public string? State { get; private set; }

        public void AppendState(string state)
        {
            State += state;
        }
    }

    public class ChainFooMiddleware : BaseQueueMiddleware
    {
        private readonly ChainState _state;

        public ChainFooMiddleware(ChainState state)
        {
            _state = state;
        }

        public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T>? callback = null)
        {
            _state.AppendState("foo");
            return base.PublishAsync(message, messageContext, callback);
        }
    }

    public class ChainBarMiddleware : BaseQueueMiddleware
    {
        private readonly ChainState _state;

        public ChainBarMiddleware(ChainState state)
        {
            _state = state;
        }

        public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T>? callback = null)
        {
            _state.AppendState("bar");
            return base.PublishAsync(message, messageContext, callback);
        }
    }

    public class CountMiddleware : BaseQueueMiddleware
    {
        public int Published { get; private set; }
        public int Received { get; private set; }

        public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T>? callback = null)
        {
            Published++;
            return base.PublishAsync(message, messageContext, callback);
        }

        public override async Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
            ReceiveAsyncDelegate<T>? callback = null)
        {
            var result = await base.ReceiveAsync(message, messageContext, callback);
            if (result)
            {
                Received++;
            }

            return result;
        }
    }
}
