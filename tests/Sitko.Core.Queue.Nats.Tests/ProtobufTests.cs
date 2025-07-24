using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Queue.Nats.Tests;

public class ProtobufTests : BaseTest<NatsQueueTestScope>
{
    public ProtobufTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Protobuf()
    {
        var scope = await GetScopeAsync();

        var queue = scope.GetService<IQueue>();

        string? receivedText = null;
        await queue.SubscribeAsync<TestMsg>((message, _) =>
        {
            receivedText = message.Data;
            return Task.FromResult(true);
        });

        var msg = new TestMsg { Data = Guid.NewGuid().ToString() };

        await queue.PublishAsync(msg);

        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

        Assert.NotNull(receivedText);
        Assert.Equal(msg.Data, receivedText);
    }
}
