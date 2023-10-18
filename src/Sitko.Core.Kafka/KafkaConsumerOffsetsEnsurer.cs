using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Kafka;

internal class KafkaConsumerOffsetsEnsurer
{
    private readonly ILogger<KafkaConsumerOffsetsEnsurer> logger;

    public KafkaConsumerOffsetsEnsurer(ILogger<KafkaConsumerOffsetsEnsurer> logger) => this.logger = logger;

    public async Task EnsureOffsetsAsync(KafkaConfigurator configurator, KafkaModuleOptions options)
    {
        var adminClient = GetAdminClient(options.Brokers);
        foreach (var consumer in configurator.Consumers)
        {
            foreach (var topic in consumer.Topics)
            {
                await EnsureTopicOffsetsAsync(consumer, adminClient, topic, options.Brokers);
            }
        }
    }

    private async Task EnsureTopicOffsetsAsync(ConsumerRegistration consumer, IAdminClient adminClient, TopicInfo topic,
        string[] brokers)
    {
        logger.LogDebug("Try to create topic {Topic}", topic);
        try
        {
            await adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = topic.Name,
                    NumPartitions = topic.PartitionsCount,
                    ReplicationFactor = topic.ReplicationFactor
                }
            });
            logger.LogInformation("Topic {Topic} created", topic);
        }
        catch (Exception ex)
        {
            if (ex is CreateTopicsException createTopicsException &&
                createTopicsException.Results.First().Error.Reason.Contains("already exists"))
            {
                logger.LogDebug("Topic {Topic} already exists", topic);
            }
            else
            {
                logger.LogError(ex, "Error creating topic {Topic}: {ErrorText}", topic.Name, ex.Message);
                return;
            }
        }

        var topicInfo = adminClient.GetMetadata(topic.Name, TimeSpan.FromSeconds(30)).Topics.First();
        if (topicInfo is null || !topicInfo.Partitions.Any())
        {
            logger.LogError("Still no metadata for topic {Topic}", topic.Name);
            return;
        }

        var partitions = topicInfo.Partitions.Select(metadata =>
            new TopicPartition(topic.Name, new Partition(metadata.PartitionId))).ToList();
        var commited = await adminClient.ListConsumerGroupOffsetsAsync(new[]
        {
            new ConsumerGroupTopicPartitions(consumer.GroupId, partitions)
        });
        if (!commited.Any())
        {
            logger.LogWarning(
                "Can't find offsets for group {ConsumerGroup} and topic {Topic}",
                consumer.GroupId, topic);
            return;
        }

        var badPartitions = commited.First().Partitions.Where(
            partitionOffset =>
                partitionOffset.Offset == Offset.Unset
        ).Select(error => error.Partition).ToList();
        if (badPartitions.Any())
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = string.Join(",", brokers), GroupId = consumer.GroupId
            };
            var confluentConsumer = new ConsumerBuilder<byte[], byte[]>(consumerConfig).Build();
            var toCommit = new List<TopicPartitionOffset>();
            foreach (var partition in badPartitions)
            {
                var topicPartition = new TopicPartition(topic.Name, partition);
                var partitionOffset =
                    confluentConsumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(30));
                var newOffset = new TopicPartitionOffset(topicPartition, partitionOffset.High);
                logger.LogWarning(
                    "Ensure {Partition} offset for consumer {ConsumerGroup}: {Offset}",
                    partition, consumer.GroupId, newOffset.Offset);
                toCommit.Add(newOffset);
            }

            confluentConsumer.Commit(toCommit);
            logger.LogInformation("Offsets for consumer group {ConsumerGroupName} in topic {Topic} ensured",
                consumer.GroupId, topic);
        }
        else
        {
            logger.LogDebug(
                "No partitions without stored offsets in topic {Topic} for consumer group {ConsumerGroupName}", topic,
                consumer.GroupId);
        }
    }

    private IAdminClient GetAdminClient(string[] brokers)
    {
        var adminClientConfig = new AdminClientConfig
        {
            BootstrapServers = string.Join(",", brokers), ClientId = "AdminClient"
        };
        var adminClient = new AdminClientBuilder(adminClientConfig)
            .SetLogHandler((_, m) => logger.LogInformation("{Message}", m.Message))
            .SetErrorHandler((_, error) => logger.LogError("Kafka Consumer Error: {Error}", error))
            .Build();


        return adminClient;
    }
}
