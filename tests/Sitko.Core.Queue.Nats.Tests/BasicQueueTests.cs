using Sitko.Core.Queue.Exceptions;
using Sitko.Core.Queue.Tests;
using Xunit;

namespace Sitko.Core.Queue.Nats.Tests;

public class
    BasicQueueTests : BasicQueueTests<NatsQueueTestScope, NatsQueueModule, NatsQueue, NatsQueueModuleOptions>
{
    public BasicQueueTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task RequestResponseTimeout()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        var msg = new TestMessage();
        var timeout = TimeSpan.FromMilliseconds(500);
        var subResult = await queue.ReplyAsync<TestMessage, TestResponse>(async (message, _) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new TestResponse { Id = message.Id };
        });
        Assert.True(subResult.IsSuccess);
        var ex = await Assert.ThrowsAsync<QueueRequestTimeoutException>(() =>
            queue.RequestAsync<TestMessage, TestResponse>(msg, null, timeout));

        Assert.Equal(timeout, ex.Timeout);
    }
}
