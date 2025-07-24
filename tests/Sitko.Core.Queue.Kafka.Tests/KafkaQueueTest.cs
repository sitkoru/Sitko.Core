using FluentAssertions;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;

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

    [Fact]
    public async Task ProduceBatch()
    {
        var scope = await GetScopeAsync();
        var producer = scope.GetService<IEventProducer>();
        var range = Enumerable.Range(1, 100);
        var testEvents =
            range.Select(_ => new BatchTestEvent { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() });
        foreach (var testEvent in testEvents)
        {
            producer.Produce($"test{testEvent.Id}", testEvent);
        }

        await Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        EventRegistrator.BatchesMessagesCount.Should().Be(100);
        EventRegistrator.BatchesCount.Should().Be(10);
    }
}
