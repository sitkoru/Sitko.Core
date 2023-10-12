using System.Collections.Concurrent;
using System.Reflection;
using Confluent.Kafka;
using KafkaFlow.Consumers;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Kafka;

internal class KafkaConsumerOffsetsEnsurer
{
    private static FieldInfo? consumerManagerField;
    private static FieldInfo? directConsumerField;
    private static PropertyInfo? consumerProperty;
    private static readonly HashSet<string> ProcessedPartitions = new();

    private static readonly
        ConcurrentDictionary<IMessageConsumer, (IConsumer kafkaFlowConsumer, IConsumer<byte[], byte[]> confluentConsumer
            )> Consumers = new();

    private readonly IConsumerAccessor consumerAccessor;
    private readonly ConcurrentDictionary<string, Task> tasks = new();
    private IAdminClient? adminClient;
    private ILogger<KafkaConsumerOffsetsEnsurer> logger;

    public KafkaConsumerOffsetsEnsurer(IConsumerAccessor consumerAccessor, ILogger<KafkaConsumerOffsetsEnsurer> logger)
    {
        this.consumerAccessor = consumerAccessor;
        this.logger = logger;
    }

    private IAdminClient GetAdminClient(string[] brokers)
    {
        if (adminClient is null)
        {
            var adminClientConfig = new AdminClientConfig
            {
                BootstrapServers = string.Join(",", brokers), ClientId = "AdminClient"
            };
            adminClient = new AdminClientBuilder(adminClientConfig)
                .SetLogHandler((_, m) => logger.LogInformation("{Message}", m.Message))
                .SetErrorHandler((_, error) => logger.LogError("Kafka Consumer Error: {Error}", error))
                .Build();
        }

        return adminClient;
    }

    public void EnsureOffsets(
        string[] brokers,
        string name,
        List<TopicPartition> list
    )
    {
        foreach (var partition in list)
        {
            var key = $"{name}/{partition.Partition.Value}";
            if (ProcessedPartitions.Contains(key))
            {
                continue;
            }

            tasks.GetOrAdd(
                key, _ => { return Task.Run(async () => await ProcessPartition(brokers, name, partition)); }
            );
            ProcessedPartitions.Add(key);
        }
    }

    private async Task ProcessPartition(string[] brokers, string name, TopicPartition partition)
    {
        var messageConsumer = consumerAccessor.GetConsumer(name);
        messageConsumer.Pause(new[] { partition });
        try
        {
            var (kafkaFlowConsumer, confluentConsumer) = GetConsumers(messageConsumer);

            var commited = await GetAdminClient(brokers).ListConsumerGroupOffsetsAsync(new[]
            {
                new ConsumerGroupTopicPartitions(messageConsumer.GroupId, new List<TopicPartition> { partition })
            });
            if (!commited.Any())
            {
                logger.LogWarning(
                    "Не получилось найти оффсеты для назначенных партиций консьюмера {Consumer}",
                    messageConsumer.ConsumerName);
                return;
            }

            var currentOffset = commited.First().Partitions.FirstOrDefault(
                partitionOffset =>
                    partitionOffset.TopicPartition == partition
            );

            if (currentOffset is null || currentOffset.Offset == Offset.Unset)
            {
                var partitionOffset = confluentConsumer.QueryWatermarkOffsets(partition, TimeSpan.FromSeconds(30));
                var newOffset = new TopicPartitionOffset(partition, partitionOffset.High);
                logger.LogWarning(
                    "Сохраняем отсутствующий оффсет для партиции {Partition} консьюмера {Consumer}: {Offset}",
                    partition, name, newOffset.Offset);
                kafkaFlowConsumer.Commit(new[] { newOffset });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error process partition {Partition}: {Error}", partition, ex);
            throw;
        }
        finally
        {
            messageConsumer.Resume(new[] { partition });
        }
    }

    private static (IConsumer kafkaFlowConsumer, IConsumer<byte[], byte[]> confluentConsumer) GetConsumers(
        IMessageConsumer consumer) =>
        Consumers.GetOrAdd(
            consumer, messageConsumer =>
            {
                consumerManagerField ??= messageConsumer.GetType().GetField(
                                             "consumerManager",
                                             BindingFlags.Instance |
                                             BindingFlags.NonPublic
                                         ) ??
                                         throw new InvalidOperationException(
                                             "Can't find field consumerManager"
                                         );
                var consumerManager =
                    consumerManagerField.GetValue(messageConsumer) ??
                    throw new InvalidOperationException(
                        "Can't get consumerManager"
                    );
                consumerProperty ??= consumerManager.GetType()
                                         .GetProperty(
                                             "Consumer",
                                             BindingFlags.Instance |
                                             BindingFlags.Public
                                         ) ??
                                     throw new InvalidOperationException(
                                         "Can't find field consumer"
                                     );
                var flowConsumer =
                    consumerProperty.GetValue(consumerManager) as IConsumer ??
                    throw new InvalidOperationException(
                        "Can't get flowConsumer"
                    );

                directConsumerField ??= flowConsumer.GetType()
                                            .GetField(
                                                "consumer",
                                                BindingFlags.Instance |
                                                BindingFlags.NonPublic
                                            ) ??
                                        throw new InvalidOperationException(
                                            "Can't find field directConsumer"
                                        );
                var confluentConsumer =
                    directConsumerField.GetValue(flowConsumer) as
                        IConsumer<byte[], byte[]> ??
                    throw new InvalidOperationException(
                        "Can't getdirectConsumer"
                    );

                return (flowConsumer, confluentConsumer);
            }
        );
}
