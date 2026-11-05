namespace Sitko.Core.Queue.Kafka.Tests.Data
{
    [QueueEvent("RetryTestEvents", "86B72495-534A-4081-8A21-5177286A4217")]
    public sealed class RetryTestEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }
}
