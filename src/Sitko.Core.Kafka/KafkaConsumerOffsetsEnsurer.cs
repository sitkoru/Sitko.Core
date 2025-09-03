using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using Sitko.Core.App.Helpers;

namespace Sitko.Core.Kafka;

internal class KafkaConsumerOffsetsEnsurer(
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<KafkaConsumerOffsetsEnsurer> logger)
{
    private readonly TimeSpan lockTimeout = TimeSpan.FromSeconds(30);

    public async Task EnsureOffsetsAsync(KafkaConfigurator configurator, KafkaModuleOptions options)
    {
        var adminClient = GetAdminClient(options);
        var tasks = new List<Task>();
        foreach (var consumer in configurator.Consumers)
        {
            tasks.AddRange(consumer.Topics.Select(topic =>
                Task.Run(async () => await EnsureTopicOffsetsAsync(consumer, adminClient, topic, options))));
        }

        await Task.WhenAll(tasks);
        adminClient.Dispose();
    }

    private async Task EnsureTopicOffsetsAsync(ConsumerRegistration consumer, IAdminClient adminClient, TopicInfo topic,
        KafkaModuleOptions options)
    {
        logger.LogDebug("Try to create topic {Topic}", topic);
        try
        {
            await adminClient.CreateTopicsAsync([
                new TopicSpecification
                {
                    Name = topic.Name,
                    NumPartitions = options.TopicPartitionsCount,
                    ReplicationFactor = options.TopicReplicationFactor,
                    Configs = options.TopicConfigs
                }
            ]);
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
        if (topicInfo is null || topicInfo.Partitions.Count == 0)
        {
            logger.LogError("Still no metadata for topic {Topic}", topic.Name);
            return;
        }

        var partitions = topicInfo.Partitions.Select(metadata =>
            new TopicPartition(topic.Name, new Partition(metadata.PartitionId))).ToList();
        var committed = await adminClient.ListConsumerGroupOffsetsAsync([
            new ConsumerGroupTopicPartitions(consumer.GroupId, partitions)
        ]);
        if (committed.Count == 0)
        {
            logger.LogWarning(
                "Can't find offsets for group {ConsumerGroup} and topic {Topic}",
                consumer.GroupId, topic);
            return;
        }

        var badPartitions = committed.First().Partitions.Where(partitionOffset =>
            partitionOffset.Offset == Offset.Unset
        ).Select(error => error.Partition).ToList();

        if (badPartitions.Count != 0)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = string.Join(",", options.Brokers),
                GroupId = consumer.GroupId,
                EnableAutoCommit = false
            };
            if (options.UseSaslAuth)
            {
                consumerConfig.SaslPassword = options.SaslPassword;
                consumerConfig.SaslUsername = options.SaslUserName;
                consumerConfig.SaslMechanism = (SaslMechanism?)options.SaslMechanisms;
                consumerConfig.SecurityProtocol = (SecurityProtocol?)options.SecurityProtocol;
                if (consumerConfig.SecurityProtocol == SecurityProtocol.SaslSsl)
                {
                    consumerConfig.SslCaLocation = CertHelper.GetCertPath(options.SaslCertBase64);
                }
            }

            var cts = new CancellationTokenSource();
            using var confluentConsumer = new ConsumerBuilder<byte[], byte[]>(consumerConfig)
                .SetPartitionsAssignedHandler((_, _) => { cts.Cancel(); })
                .Build();
            var pipeline = pipelineProvider.GetPipeline(nameof(KafkaConsumerOffsetsEnsurer));
            var watermarkOffsetsTasks = badPartitions.Select(partition =>
                GetWatermarkOffsetsAsync(topic, partition, confluentConsumer, consumer.GroupId, pipeline)).ToList();
            await Task.WhenAll(watermarkOffsetsTasks);
            var toCommit = watermarkOffsetsTasks.Select(task => task.Result);

            // Add consumer to group
            confluentConsumer.Subscribe(topic.Name);
            // Wait for rebalance
            try
            {
                confluentConsumer.Consume(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Rebalance complete
            }

            // Commit offsets
            confluentConsumer.Commit(toCommit);
            // Remove consumer from group and trigger rebalance
            confluentConsumer.Close();

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

    private async Task<TopicPartitionOffset> GetWatermarkOffsetsAsync(TopicInfo topic, int partition,
        IConsumer<byte[], byte[]> consumer,
        string groupId, ResiliencePipeline pipeline)
    {
        var topicPartition = new TopicPartition(topic.Name, partition);
        var partitionOffset = await pipeline.ExecuteAsync(_ =>
        {
            var offsets = consumer.QueryWatermarkOffsets(topicPartition, lockTimeout);
            return ValueTask.FromResult(offsets);
        });

        var newOffset = new TopicPartitionOffset(topicPartition, partitionOffset.High);
        logger.LogWarning("Ensure {Partition} offset for consumer {GroupId}: {Offset}", partition,
            groupId, newOffset.Offset);
        return newOffset;
    }

    private IAdminClient GetAdminClient(KafkaModuleOptions options)
    {
        var adminClientConfig = new AdminClientConfig
        {
            BootstrapServers = string.Join(",", options.Brokers), ClientId = "AdminClient"
        };

        if (options.UseSaslAuth)
        {
            adminClientConfig.SaslPassword = options.SaslPassword;
            adminClientConfig.SaslUsername = options.SaslUserName;
            adminClientConfig.SaslMechanism = (SaslMechanism?)options.SaslMechanisms;
            adminClientConfig.SecurityProtocol = (SecurityProtocol?)options.SecurityProtocol;
            if (adminClientConfig.SecurityProtocol == SecurityProtocol.SaslSsl)
            {
                adminClientConfig.SslCaLocation = CertHelper.GetCertPath(options.SaslCertBase64);
            }
        }

        var adminClient = new AdminClientBuilder(adminClientConfig)
            .SetLogHandler((_, m) => logger.LogInformation("{Message}", m.Message))
            .SetErrorHandler((_, error) => logger.LogError("Kafka Consumer Error: {Error}", error))
            .Build();


        return adminClient;
    }
}
