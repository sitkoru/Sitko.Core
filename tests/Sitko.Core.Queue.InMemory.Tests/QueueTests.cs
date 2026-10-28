using Sitko.Core.Queue.Tests;
using Xunit;

namespace Sitko.Core.Queue.InMemory.Tests;

public class BasicQueueTests : BasicQueueTests<InMemoryQueueTestScope, InMemoryQueueModule, InMemoryQueue,
    InMemoryQueueModuleOptions>
{
    public BasicQueueTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TwoSubscriptionsReceivePublishedMessageExactlyOnce()
    {
        var scope = await GetScopeAsync();
        var queue = scope.GetService<IQueue>();
        var messageId = Guid.Parse("c8162a5d-c1d0-4768-b23a-1c937ae630fa");
        var markerId = Guid.Parse("67758f31-85e7-488f-9500-c645f570b351");
        var markerReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstDeliveryCount = 0;
        var secondDeliveryCount = 0;

        var firstSubscription = await queue.SubscribeAsync<TestMessage>((message, context) =>
        {
            if (message.Id == messageId)
            {
                _ = Interlocked.Increment(ref firstDeliveryCount);
            }
            else if (message.Id == markerId)
            {
                _ = markerReceived.TrySetResult();
            }

            return Task.FromResult(true);
        });
        Assert.True(firstSubscription.IsSuccess);

        var secondSubscription = await queue.SubscribeAsync<TestMessage>((message, context) =>
        {
            if (message.Id == messageId)
            {
                _ = Interlocked.Increment(ref secondDeliveryCount);
            }
            else if (message.Id == markerId)
            {
                _ = markerReceived.TrySetResult();
            }

            return Task.FromResult(true);
        });
        Assert.True(secondSubscription.IsSuccess);
        Assert.True((await queue.PublishAsync(new TestMessage { Id = messageId })).IsSuccess);
        Assert.True((await queue.PublishAsync(new TestMessage { Id = markerId })).IsSuccess);

        // FIFO-маркер приходит после завершения всех обработчиков предыдущего сообщения.
        await markerReceived.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Equal<int>([1, 1], [Volatile.Read(ref firstDeliveryCount), Volatile.Read(ref secondDeliveryCount)]);
    }

    [Fact]
    public async Task ConcurrentSubscriptionsReceivePublishedMessageExactlyOnce()
    {
        var scope = await GetScopeAsync();
        var queue = scope.GetService<IQueue>();
        var message = new TestMessage();
        var marker = new TestMessage();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var markerReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deliveryCounts = new int[16];
        var subscriptions = Enumerable.Range(0, deliveryCounts.Length).Select(index => Task.Run(async () =>
        {
            await start.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            return await queue.SubscribeAsync<TestMessage>((received, context) =>
            {
                if (received.Id == message.Id)
                {
                    _ = Interlocked.Increment(ref deliveryCounts[index]);
                }
                else if (received.Id == marker.Id)
                {
                    _ = markerReceived.TrySetResult();
                }

                return Task.FromResult(true);
            });
        }, TestContext.Current.CancellationToken)).ToArray();
        start.SetResult();
        var results = await Task.WhenAll(subscriptions)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.All(results, result => Assert.True(result.IsSuccess));

        Assert.True((await queue.PublishAsync(message)).IsSuccess);
        Assert.True((await queue.PublishAsync(marker)).IsSuccess);
        await markerReceived.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.All(deliveryCounts, count => Assert.Equal(1, count));
    }

    [Fact]
    public async Task UnsubscribingOneSubscriptionPreservesOtherAndAllowsResubscription()
    {
        var scope = await GetScopeAsync();
        var queue = scope.GetService<IQueue>();
        var message = new TestMessage();
        var marker = new TestMessage();
        var markerReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var removedCount = 0;
        var remainingCount = 0;
        var first = await queue.SubscribeAsync<TestMessage>((_, _) =>
        {
            _ = Interlocked.Increment(ref removedCount);
            return Task.FromResult(true);
        });
        var second = await queue.SubscribeAsync<TestMessage>((received, context) =>
        {
            if (received.Id == message.Id)
            {
                _ = Interlocked.Increment(ref remainingCount);
            }
            else if (received.Id == marker.Id)
            {
                _ = markerReceived.TrySetResult();
            }

            return Task.FromResult(true);
        });
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        await queue.UnsubscribeAsync<TestMessage>(first.SubscriptionId);
        Assert.True((await queue.PublishAsync(message)).IsSuccess);
        Assert.True((await queue.PublishAsync(marker)).IsSuccess);
        await markerReceived.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(0, Volatile.Read(ref removedCount));
        Assert.Equal(1, Volatile.Read(ref remainingCount));

        await queue.UnsubscribeAsync<TestMessage>(second.SubscriptionId);
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resubscribedCount = 0;
        var third = await queue.SubscribeAsync<TestMessage>((received, context) =>
        {
            if (received.Id == message.Id)
            {
                _ = Interlocked.Increment(ref resubscribedCount);
            }
            else if (received.Id == marker.Id)
            {
                _ = delivered.TrySetResult();
            }
            return Task.FromResult(true);
        });
        Assert.True(third.IsSuccess);
        Assert.True((await queue.PublishAsync(message)).IsSuccess);
        Assert.True((await queue.PublishAsync(marker)).IsSuccess);
        await delivered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(1, Volatile.Read(ref resubscribedCount));
        Assert.Equal(0, Volatile.Read(ref removedCount));
        Assert.Equal(1, Volatile.Read(ref remainingCount));
    }

    [Fact]
    public async Task UnsubscribingLastSubscriptionPreservesReplyHandler()
    {
        var scope = await GetScopeAsync();
        var queue = scope.GetService<IQueue>();
        var reply = await queue.ReplyAsync<TestMessage, TestResponse>(static (message, _) =>
            Task.FromResult(new TestResponse { Id = message.Id }));
        var subscription = await queue.SubscribeAsync<TestMessage>(static (_, _) => Task.FromResult(true));
        Assert.True(reply.IsSuccess);
        Assert.True(subscription.IsSuccess);

        await queue.UnsubscribeAsync<TestMessage>(subscription.SubscriptionId);
        var request = new TestMessage();
        var response = await queue.RequestAsync<TestMessage, TestResponse>(request,
            timeout: TimeSpan.FromSeconds(10));

        _ = Assert.NotNull(response);
        Assert.Equal(request.Id, response.Value.message.Id);
        Assert.True(await queue.StopReplyAsync<TestMessage, TestResponse>(reply.SubscriptionId));
    }

}
