using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Tests;

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
        var processor = scope.GetService<FooTestMessageProcessor>();
        Assert.NotNull(processor);

        var counter = scope.GetService<TestQueueProcessorCounter>();
        Assert.NotNull(counter);

        Assert.Equal(0, counter.Count);

        var queue = scope.GetService<IQueue>();

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
        var counter = scope.GetService<TestQueueProcessorCounter>();
        Assert.NotNull(counter);

        Assert.Equal(0, counter.Count);

        var queue = scope.GetService<IQueue>();

        var msg = new TestMessage();
        var result = await queue.PublishAsync(msg);
        Assert.True(result.IsSuccess);

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal(2, counter.Count);
    }
}

public abstract class TestQueueProcessor<T> : IQueueProcessor<T> where T : class, new()
{
    private readonly TestQueueProcessorCounter counter;

    protected TestQueueProcessor(TestQueueProcessorCounter counter) => this.counter = counter;

    public Task<bool> ProcessAsync(T message, QueueMessageContext messageContext)
    {
        counter.Count++;
        return Task.FromResult(true);
    }
}

public class FooTestMessageProcessor : TestQueueProcessor<TestMessage>
{
    public FooTestMessageProcessor(TestQueueProcessorCounter counter) : base(counter)
    {
    }
}

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
        IAppEnvironment environment,
        IServiceCollection services, string name) =>
        base.ConfigureServices(configuration, environment, services, name)
            .AddSingleton<TestQueueProcessorCounter>();

    protected override void Configure(IConfiguration configuration, IAppEnvironment environment,
        TestQueueOptions options, string name) =>
        options.RegisterProcessor<FooTestMessageProcessor, TestMessage>();
}

public class MultipleProcessorQueueTestScope : BaseTestQueueTestScope
{
    protected override IServiceCollection ConfigureServices(IConfiguration configuration,
        IAppEnvironment environment,
        IServiceCollection services, string name) =>
        base.ConfigureServices(configuration, environment, services, name)
            .AddSingleton<TestQueueProcessorCounter>();

    protected override void Configure(IConfiguration configuration, IAppEnvironment environment,
        TestQueueOptions options, string name) =>
        options.RegisterProcessors<ProcessorTests>();
}
