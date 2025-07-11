namespace Sitko.Core.Queue.Kafka.Tests.Data;

[QueueEvent("TestEvents", "89682D87-0F10-4DFE-AA6A-BEF315D05425")]
public class TestEvent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "TestEvent";
}
