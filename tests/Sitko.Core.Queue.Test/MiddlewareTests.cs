using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Tests;

public class MiddlewareTests : BaseTest
{
    public MiddlewareTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Publish()
    {
        var scope = await GetScopeAsync<MiddlewareQueueTestScope>();

        var queue = scope.GetService<IQueue>();

        var mw = scope.GetService<CountMiddleware>();

        Assert.Equal(0, mw.Published);

        var publishResult = await queue.PublishAsync(new TestMessage());
        Assert.True(publishResult.IsSuccess);

        Assert.Equal(1, mw.Published);
    }

    [Fact]
    public async Task Receive()
    {
        var scope = await GetScopeAsync<MiddlewareQueueTestScope>();

        var queue = scope.GetService<IQueue>();

        var mw = scope.GetService<CountMiddleware>();

        Assert.Equal(0, mw.Received);

        var publishResult = await queue.PublishAsync(new TestMessage());
        Assert.True(publishResult.IsSuccess);

        Assert.Equal(0, mw.Received);

        var subResult = await queue.SubscribeAsync<TestMessage>((_, _) => Task.FromResult(true));
        Assert.True(subResult.IsSuccess);

        publishResult = await queue.PublishAsync(new TestMessage());
        Assert.True(publishResult.IsSuccess);

        Assert.Equal(1, mw.Received);
    }

    [Fact]
    public async Task Multiple()
    {
        var scope = await GetScopeAsync<MultipleMiddlewareQueueTestScope>();

        var classes =
            typeof(MiddlewareTests).Assembly.ExportedTypes.Where(t => typeof(IQueueMiddleware).IsAssignableFrom(t));

        var mws = scope.GetServices<IQueueMiddleware>();

        Assert.Equal(classes.Count(), mws.Count());
    }

    [Fact]
    public async Task Chain()
    {
        var scope = await GetScopeAsync<ChainMiddlewareQueueTestScope>();

        var state = scope.GetService<ChainState>();

        Assert.Null(state.State);

        var queue = scope.GetService<IQueue>();

        var publishResult = await queue.PublishAsync(new TestMessage());
        Assert.True(publishResult.IsSuccess);

        Assert.Equal("foobar", state.State);
    }
}

public class MiddlewareQueueTestScope : BaseTestQueueTestScope
{
    protected override void Configure(IApplicationContext applicationContext,
        TestQueueOptions options, string name) =>
        options.RegisterMiddleware<CountMiddleware>();
}

public class MultipleMiddlewareQueueTestScope : BaseTestQueueTestScope
{
    protected override IServiceCollection ConfigureServices(IApplicationContext applicationContext,
        IServiceCollection services, string name) =>
        base.ConfigureServices(applicationContext, services, name).AddSingleton<ChainState>();

    protected override void Configure(IApplicationContext applicationContext,
        TestQueueOptions options, string name) =>
        options.RegisterMiddlewares<MiddlewareTests>();
}

public class ChainMiddlewareQueueTestScope : BaseTestQueueTestScope
{
    protected override IServiceCollection ConfigureServices(IApplicationContext applicationContext,
        IServiceCollection services, string name) =>
        base.ConfigureServices(applicationContext, services, name).AddSingleton<ChainState>();

    protected override void Configure(IApplicationContext applicationContext,
        TestQueueOptions options, string name)
    {
        options.RegisterMiddleware<ChainFooMiddleware>();
        options.RegisterMiddleware<ChainBarMiddleware>();
    }
}

public class ChainState
{
    public string? State { get; private set; }

    public void AppendState(string state) => State += state;
}

public class ChainFooMiddleware : BaseQueueMiddleware
{
    private readonly ChainState state;

    public ChainFooMiddleware(ChainState state) => this.state = state;

    public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
        PublishAsyncDelegate<T>? callback = null)
    {
        state.AppendState("foo");
        return base.PublishAsync(message, messageContext, callback);
    }
}

public class ChainBarMiddleware : BaseQueueMiddleware
{
    private readonly ChainState state;

    public ChainBarMiddleware(ChainState state) => this.state = state;

    public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
        PublishAsyncDelegate<T>? callback = null)
    {
        state.AppendState("bar");
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
