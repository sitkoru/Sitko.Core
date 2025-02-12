using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Sitko.Core.Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

public class KafkaQueueTestScope : BaseTestScope
{
    private const string Topic = "Test_Topic";
    private const string Producer = "Test_Producer";
    private const string Cluster = "Test_Queue_Cluster";

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.AddKafkaQueue(Cluster, configurator =>
        {
            configurator
                .AutoCreateTopic(Topic, 1, 1)
                .EnsureOffsets()
                .AddProducer(Producer, Topic)
                .RegisterEvent<TestEvent>(Topic, Producer)
                .AddConsumer<KafkaQueueTestConsumer>(hostBuilder.GetSitkoCore().Context,
                [new TopicInfo(Topic, 1, 1)]);
        }, options =>
        {
            options.Brokers = ["localhost:9092"];
        });
        return hostBuilder;
    }
}
