using FluentAssertions;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Kafka.Tests;

public class KafkaQueueControllerTest(ITestOutputHelper testOutputHelper)
    : BaseKafkaQueueTest<StoppedKafkaQueueTestScope>(testOutputHelper)
{
    [Fact]
    public async Task StartConsumers()
    {
        var scope = await GetScopeAsync();
        var producer = scope.GetService<IEventProducer>();
        var testEvent = new TestEvent { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeFalse();
        producer.Produce("test", testEvent);
        await Task.Delay(TimeSpan.FromSeconds(2));
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeFalse();

        var controller = scope.GetService<IKafkaQueueController>();
        await controller.StartAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
        EventRegistrator.IsRegistered(testEvent.Id).Should().BeTrue();
    }
}
