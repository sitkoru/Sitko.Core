using AwesomeAssertions;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

public class KafkaQueueTest(ITestOutputHelper testOutputHelper) : BaseKafkaQueueTest(testOutputHelper)
{
    [Fact]
    public async Task Produce()
    {
        var scope = await GetScopeAsync();
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeFalse();
        var kafkaFlowProducer = scope.GetService<IEventProducer>();
        kafkaFlowProducer.Produce("test", testEvent);
        await EventRegistrator.WaitUntilAsync(() => EventRegistrator.IsRegistered(testEvent.Id),
            TestContext.Current.CancellationToken);
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeTrue();
    }

    [Fact]
    public async Task ProduceBatch()
    {
        var scope = await GetScopeAsync();
        var producer = scope.GetService<IEventProducer>();
        var testEvents = Enumerable.Range(1, 100)
            .Select(static _ => new BatchTestEvent { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() })
            .ToArray();
        var expectedIds = testEvents.Select(static message => message.Id).ToArray();
        TestOutputHelper.WriteLine($"Expected batch IDs: {string.Join(",", expectedIds)}");
        foreach (var testEvent in testEvents.Take(5))
        {
            producer.Produce($"test{testEvent.Id}", testEvent);
        }

        // Последующие сообщения удерживаются до передачи неполного пакета по таймеру.
        await EventRegistrator.WaitUntilAsync(static () => EventRegistrator.BatchesCount > 0,
            TestContext.Current.CancellationToken);
        var firstBatches = EventRegistrator.GetBatches();
        _ = firstBatches.Should().NotBeEmpty();
        foreach (var batch in firstBatches)
        {
            _ = batch.Length.Should().BeInRange(1, 5);
        }
        TestOutputHelper.WriteLine($"Initial batch sizes: {string.Join(",", firstBatches.Select(static batch => batch.Length))}");

        foreach (var testEvent in testEvents.Skip(5))
        {
            producer.Produce($"test{testEvent.Id}", testEvent);
        }

        await EventRegistrator.WaitUntilAsync(static () => EventRegistrator.BatchesMessagesCount >= 100,
            TestContext.Current.CancellationToken);
        var batches = EventRegistrator.GetBatches();
        var observedIds = batches.SelectMany(static batch => batch).ToArray();
        TestOutputHelper.WriteLine($"Observed batches: {string.Join(";", batches.Select(static batch => string.Join(",", batch)))}");
        _ = observedIds.Should().HaveCount(100).And.OnlyHaveUniqueItems().And.BeEquivalentTo(expectedIds);
        foreach (var batch in batches)
        {
            _ = batch.Length.Should().BeInRange(1, 10);
        }
    }
}
