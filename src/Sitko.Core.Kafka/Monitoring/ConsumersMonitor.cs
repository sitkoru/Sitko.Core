using System.Collections.Concurrent;
using KafkaFlow.Consumers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Kafka.Monitoring;

public class ConsumersMonitor(
    IConsumerAccessor consumerAccessor,
    IOptionsMonitor<KafkaModuleOptions> options,
    ILogger<ConsumersMonitor> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> consumersWithoutAssignments = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(options.CurrentValue.ConsumersMonitoringInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // application stopped
                return;
            }

            try
            {
                if (options.CurrentValue.RestartStoppedConsumers)
                {
                    await RestartStoppedConsumersAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error restarting consumers: {ErrorMessage}", ex.ToString());
            }
        }
    }

    private async Task RestartStoppedConsumersAsync()
    {
        foreach (var consumer in consumerAccessor.All.Where(consumer => !consumer.ManagementDisabled))
        {
            if (DisabledConsumers.IsDisabled(consumer))
            {
                continue;
            }

            if (consumer.Assignment.Any())
            {
                logger.LogDebug("Consumer {ConsumerName} has {AssignmentCount} partitions", consumer.ConsumerName,
                    consumer.Assignment.Count);
                continue;
            }

            logger.LogInformation("Consumer {ConsumerName} has no partitions", consumer.ConsumerName);
            var date = consumersWithoutAssignments.GetOrAdd(consumer.ConsumerName, _ => DateTimeOffset.UtcNow);
            if (date < DateTimeOffset.UtcNow + options.CurrentValue.StoppedConsumersWaitForAssignmentsInterval)
            {
                logger.LogWarning(
                    "Consumer {ConsumerName} has no partitions for longer than {Interval}. Restarting...",
                    consumer.ConsumerName, options.CurrentValue.StoppedConsumersWaitForAssignmentsInterval
                );

                // add jitter
                await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 10)));
                await consumer.RestartAsync();
                logger.LogWarning("Consumer {ConsumerName} restarted", consumer.ConsumerName);
                consumersWithoutAssignments.TryRemove(consumer.ConsumerName, out _);
            }
        }
    }
}
