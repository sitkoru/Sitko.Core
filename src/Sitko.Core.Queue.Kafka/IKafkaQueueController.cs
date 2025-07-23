namespace Sitko.Core.Queue.Kafka;

public interface IKafkaQueueController
{
    public Task StartAsync();
    public Task StopAsync();
}
