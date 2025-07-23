namespace Sitko.Core.Queue.Kafka.Tests.Data;

[QueueEvent("BatchTestEvents", "5B1177F4-9A07-4EA5-B872-BA409D3E358D")]
public class BatchTestEvent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "TestEvent";
}
