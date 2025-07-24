using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;

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

        await scope.StartApplicationAsync(TestContext.Current.CancellationToken); // need to start hosted services
        var processor = scope.GetService<FooTestMessageProcessor>();
        Assert.NotNull(processor);

        var counter = scope.GetService<TestQueueProcessorCounter>();
        Assert.NotNull(counter);

        Assert.Equal(0, counter.Count);

        var queue = scope.GetService<IQueue>();

        var msg = new TestMessage();
        var result = await queue.PublishAsync(msg);
        Assert.True(result.IsSuccess);

        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task MultipleProcessors()
    {
        var scope = await GetScopeAsync<MultipleProcessorQueueTestScope>();

        await scope.StartApplicationAsync(TestContext.Current.CancellationToken); // need to start hosted services
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
    protected override IHostApplicationBuilder ConfigureServices(IHostApplicationBuilder builder, string name)
    {
        base.ConfigureServices(builder, name);
        builder.Services.AddSingleton<TestQueueProcessorCounter>();
        return builder;
    }

    protected override void Configure(IApplicationContext applicationContext,
        TestQueueOptions options, string name) =>
        options.RegisterProcessor<FooTestMessageProcessor, TestMessage>();
}

public class MultipleProcessorQueueTestScope : BaseTestQueueTestScope
{
    protected override IHostApplicationBuilder ConfigureServices(IHostApplicationBuilder builder, string name)
    {
        base.ConfigureServices(builder, name);
        builder.Services.AddSingleton<TestQueueProcessorCounter>();
        return builder;
    }

    protected override void Configure(IApplicationContext applicationContext,
        TestQueueOptions options, string name) =>
        options.RegisterProcessors<ProcessorTests>();
}
