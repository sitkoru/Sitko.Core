using FluentAssertions;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

public class KafkaQueueControllerTest(ITestOutputHelper testOutputHelper)
    : BaseKafkaQueueTest<StoppedKafkaQueueTestScope>(testOutputHelper)
{
    [Fact]
    public async Task StartConsumers()
    {
        var scope = await GetScopeAsync();
        var controller = scope.GetService<IKafkaQueueController>();
        var producer = scope.GetService<IEventProducer>();
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeFalse();
        producer.Produce("test", testEvent);
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeFalse();

        await controller.StartAsync();
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeTrue();
    }
}
