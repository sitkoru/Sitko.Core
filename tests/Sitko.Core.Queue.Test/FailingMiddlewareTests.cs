using Sitko.Core.App;
using Xunit;

namespace Sitko.Core.Queue.Tests;

public class FailingMiddlewareTests : BaseTestQueueTest<FailingMiddlewareQueueTestScope>
{
    public FailingMiddlewareTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task FailingPublish()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        var publishResult = await queue.PublishAsync(new TestMessage());
        Assert.False(publishResult.IsSuccess);
    }

    [Fact]
    public async Task FailingReceive()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        var mw = scope.GetService<FailingMiddleware>();


        var received = false;
        await queue.SubscribeAsync<TestMessage>((_, _) =>
        {
            received = true;
            return Task.FromResult(true);
        });

        mw.FailOnPublish = false;
        var publishResult = await queue.PublishAsync(new TestMessage());
        Assert.True(publishResult.IsSuccess);
        Assert.False(received);

        mw.FailOnReceive = false;

        publishResult = await queue.PublishAsync(new TestMessage());
        Assert.True(publishResult.IsSuccess);
        Assert.True(received);
    }
}

public class FailingMiddlewareQueueTestScope : BaseTestQueueTestScope
{
    protected override void Configure(IApplicationContext applicationContext,
        TestQueueOptions options, string name) =>
        options.RegisterMiddleware<FailingMiddleware>();
}

public class FailingMiddleware : BaseQueueMiddleware
{
    public bool FailOnPublish { get; set; } = true;
    public bool FailOnReceive { get; set; } = true;

    public override Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<QueuePublishResult>>? callback = null)
    {
        if (!FailOnPublish)
        {
            return base.PublishAsync(message, messageContext, callback);
        }

        var result = new QueuePublishResult();
        result.SetError("Middleware failed publish");
        return Task.FromResult(result);
    }

    public override Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<bool>>? callback = null) =>
        FailOnReceive ? Task.FromResult(false) : base.ReceiveAsync(message, messageContext, callback);
}
