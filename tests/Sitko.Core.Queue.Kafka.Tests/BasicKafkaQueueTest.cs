using Sitko.Core.Queue.Kafka.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Kafka.Tests;

public class BasicKafkaQueueTest
    (ITestOutputHelper testOutputHelper)
    : BaseTest<KafkaQueueTestScope>(testOutputHelper)
{

    [Fact]
    public async Task TestQueueAsync()
    {
        var scope = await GetScopeAsync();
        var kafkaQueue = scope.GetService<KafkaQueue>();

        var result = await kafkaQueue.PublishAsync
            (TestMessageData.Message, TestMessageData.MessageContext);

        Assert.True(result.IsSuccess);
    }

}
