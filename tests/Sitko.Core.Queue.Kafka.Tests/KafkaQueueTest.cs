using FluentAssertions;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Kafka.Tests;

public class KafkaQueueTest(ITestOutputHelper testOutputHelper) : BaseKafkaQueueTest(testOutputHelper)
{
    [Fact]
    public async Task Produce()
    {
        var scope = await GetScopeAsync();
        var producer = scope.GetService<IEventProducer>();
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeFalse();
        producer.Produce("test", testEvent);
        await Task.Delay(TimeSpan.FromSeconds(5));
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeTrue();
    }
}
