using KafkaFlow.Consumers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Queue.Kafka;

public class KafkaQueueController(
    IConsumerAccessor consumerAccessor,
    IOptionsMonitor<KafkaQueueModuleOptions> options,
    ILogger<KafkaQueueController> logger)
    : IKafkaQueueController
{
    private readonly IMessageConsumer[] consumers = consumerAccessor.All.Where(c =>
        c.ClusterName.Equals(options.CurrentValue.ClusterName, StringComparison.OrdinalIgnoreCase)).ToArray();

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Kafka queue consumers");
        foreach (var consumer in consumers)
        {
            logger.LogInformation("Starting consumer {ConsumerName}", consumer.ConsumerName);
            if (consumer.Status != ConsumerStatus.Running)
            {
                try
                {
                    await consumer.StartAsync();
                    logger.LogInformation("Consumer {ConsumerName} started", consumer.ConsumerName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Consumer {ConsumerName} failed to start: {ErrorMessage}",
                        consumer.ConsumerName, ex.Message);
                }
            }
            else
            {
                logger.LogWarning("Consumer {ConsumerName} already started", consumer.ConsumerName);
            }
        }

        logger.LogInformation("Kafka queue consumers started");
    }

    public async Task StopAsync()
    {
        logger.LogInformation("Stopping Kafka queue consumers");
        foreach (var consumer in consumers)
        {
            logger.LogInformation("Stopping consumer {ConsumerName}", consumer.ConsumerName);
            if (consumer.Status == ConsumerStatus.Running)
            {
                try
                {
                    await consumer.StopAsync();
                    logger.LogInformation("Consumer {ConsumerName} stopped", consumer.ConsumerName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Consumer {ConsumerName} failed to stop: {ErrorMessage}",
                        consumer.ConsumerName, ex.Message);
                }
            }
            else
            {
                logger.LogWarning("Consumer {ConsumerName} already stopped", consumer.ConsumerName);
            }
        }

        logger.LogInformation("Kafka queue consumers stopped");
    }
}
