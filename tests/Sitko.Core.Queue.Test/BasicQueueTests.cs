using Xunit;

namespace Sitko.Core.Queue.Tests;

public abstract class
    BasicQueueTests<T, TQueueModule, TQueue, TConfig> : BaseQueueTest<T, TQueueModule, TQueue, TConfig>
    where T : BaseQueueTestScope<TQueueModule, TQueue, TConfig>
    where TQueueModule : QueueModule<TQueue, TConfig>, new()
    where TQueue : class, IQueue
    where TConfig : QueueModuleOptions, new()
{
    protected BasicQueueTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task PubSub()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        Guid? receivedId = null;
        var subResult = await queue.SubscribeAsync<TestMessage>((message, _) =>
        {
            receivedId = message.Id;
            return Task.FromResult(true);
        });
        Assert.True(subResult.IsSuccess);

        var msg = new TestMessage();

        var result = await queue.PublishAsync(msg);
        Assert.True(result.IsSuccess);

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.NotNull(receivedId);
        Assert.Equal(msg.Id, receivedId);
    }

    [Fact]
    public async Task PubSubMultiple()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        Guid? receivedId1 = null;
        var subResult = await queue.SubscribeAsync<TestMessage>((message, _) =>
        {
            receivedId1 = message.Id;
            return Task.FromResult(true);
        });
        Assert.True(subResult.IsSuccess);

        Guid? receivedId2 = null;
        subResult = await queue.SubscribeAsync<TestMessage>((message, _) =>
        {
            receivedId2 = message.Id;
            return Task.FromResult(true);
        });
        Assert.True(subResult.IsSuccess);

        var msg = new TestMessage();

        var result = await queue.PublishAsync(msg);
        Assert.True(result.IsSuccess);

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.NotNull(receivedId1);
        Assert.NotNull(receivedId2);
        Assert.Equal(msg.Id, receivedId1);
        Assert.Equal(msg.Id, receivedId2);
    }

    [Fact]
    public async Task RequestResponse()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        var msg = new TestMessage();

        var subResult = await queue.ReplyAsync<TestMessage, TestResponse>((message, _) =>
            Task.FromResult(new TestResponse { Id = message.Id }));
        Assert.True(subResult.IsSuccess);

        var response = await queue.RequestAsync<TestMessage, TestResponse>(msg);
        Assert.NotNull(response);
        Assert.Equal(msg.Id, response.Value.message.Id);

        var unsubResult = await queue.StopReplyAsync<TestMessage, TestResponse>(subResult.SubscriptionId);
        Assert.True(unsubResult);
    }

    [Fact]
    public async Task UnSubscribe()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        Guid? receivedId = null;
        var subResult = await queue.SubscribeAsync<TestMessage>((message, _) =>
        {
            receivedId = message.Id;
            return Task.FromResult(true);
        });
        Assert.True(subResult.IsSuccess);

        var msg = new TestMessage();

        var result = await queue.PublishAsync(msg);
        Assert.True(result.IsSuccess);

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.NotNull(receivedId);

        receivedId = null;
        await queue.UnsubscribeAsync<TestMessage>(subResult.SubscriptionId);

        result = await queue.PublishAsync(msg);
        Assert.True(result.IsSuccess);

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Null(receivedId);
    }

    [Fact]
    public async Task Context()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();
        var sentContext = new QueueMessageContext
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestResponse).FullName,
            RequestId = Guid.NewGuid().ToString(),
            RootMessageId = Guid.NewGuid(),
            RootMessageDate = DateTimeOffset.UtcNow,
            ParentMessageId = Guid.NewGuid(),
            Date = DateTimeOffset.UtcNow
        };
        QueueMessageContext? receivedContext = null;
        var subResult = await queue.SubscribeAsync<TestMessage>((_, context) =>
        {
            receivedContext = context;
            return Task.FromResult(true);
        });
        Assert.True(subResult.IsSuccess);

        var msg = new TestMessage();

        var result = await queue.PublishAsync(msg, sentContext);
        Assert.True(result.IsSuccess);

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.NotNull(receivedContext);
        Assert.Equal(sentContext.Id, receivedContext?.ParentMessageId);
        Assert.Equal(sentContext.RootMessageId, receivedContext?.RootMessageId);
        Assert.Equal(sentContext.RootMessageDate, receivedContext?.RootMessageDate);
        Assert.Equal(sentContext.RequestId, receivedContext?.RequestId);
        Assert.Equal(typeof(TestMessage).FullName, receivedContext?.MessageType);
    }
}
