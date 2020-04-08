using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Tests
{
    public class ProcessorTests : BaseTest
    {
        public ProcessorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task SingleProcessor()
        {
            var scope = await GetScopeAsync<ProcessorQueueTestScope>();

            await scope.StartApplicationAsync(); // need to start hosted services
            var processor = scope.Get<FooTestMessageProcessor>();
            Assert.NotNull(processor);

            var counter = scope.Get<TestQueueProcessorCounter>();
            Assert.NotNull(counter);

            Assert.Equal(0, counter.Count);

            var queue = scope.Get<IQueue>();

            var msg = new TestMessage();
            var result = await queue.PublishAsync(msg);
            Assert.True(result.IsSuccess);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(1, counter.Count);
        }

        [Fact]
        public async Task MultipleProcessors()
        {
            var scope = await GetScopeAsync<MultipleProcessorQueueTestScope>();

            await scope.StartApplicationAsync(); // need to start hosted services
            var counter = scope.Get<TestQueueProcessorCounter>();
            Assert.NotNull(counter);

            Assert.Equal(0, counter.Count);

            var queue = scope.Get<IQueue>();

            var msg = new TestMessage();
            var result = await queue.PublishAsync(msg);
            Assert.True(result.IsSuccess);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(2, counter.Count);
        }
    }

    public abstract class TestQueueProcessor<T> : IQueueProcessor<T> where T : class, new()
    {
        private readonly TestQueueProcessorCounter _counter;

        protected TestQueueProcessor(TestQueueProcessorCounter counter)
        {
            _counter = counter;
        }

        public Task<bool> ProcessAsync(T message, QueueMessageContext queueMessageContext)
        {
            _counter.Count++;
            return Task.FromResult(true);
        }
    }

    public class FooTestMessageProcessor : TestQueueProcessor<TestMessage>
    {
        public FooTestMessageProcessor(TestQueueProcessorCounter counter) : base(counter)
        {
        }
    }

    [UsedImplicitly]
    public class BarTestMessageProcessor : TestQueueProcessor<TestMessage>
    {
        public BarTestMessageProcessor(TestQueueProcessorCounter counter) : base(counter)
        {
        }
    }

    public class TestQueueProcessorCounter
    {
        public int Count { get; set; }
    }

    public class ProcessorQueueTestScope : BaseTestQueueTestScope
    {
        protected override IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment,
            IServiceCollection services, string name)
        {
            return base.ConfigureServices(configuration, environment, services, name)
                .AddSingleton<TestQueueProcessorCounter>();
        }

        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            TestQueueConfig config, string name)
        {
            config.RegisterProcessor<FooTestMessageProcessor, TestMessage>();
        }
    }

    public class MultipleProcessorQueueTestScope : BaseTestQueueTestScope
    {
        protected override IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment,
            IServiceCollection services, string name)
        {
            return base.ConfigureServices(configuration, environment, services, name)
                .AddSingleton<TestQueueProcessorCounter>();
        }

        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            TestQueueConfig config, string name)
        {
            config.RegisterProcessors<ProcessorTests>();
        }
    }
}
