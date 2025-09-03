using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Serilog.Events;
using Sitko.Core.App;
using Sitko.Core.Kafka;
using Sitko.Core.Queue.Kafka.Tests;
using Sitko.Core.Xunit;
using Xunit;

[assembly: AssemblyFixture(typeof(KafkaFixture))]

namespace Sitko.Core.Queue.Kafka.Tests;

public abstract class BaseKafkaQueueTest(ITestOutputHelper testOutputHelper)
    : BaseKafkaQueueTest<BaseKafkaQueueTestScope>(testOutputHelper);

public abstract class BaseKafkaQueueTest<T>(ITestOutputHelper testOutputHelper)
    : BaseTest<T>(testOutputHelper) where T : BaseKafkaQueueTestScope;

public class BaseKafkaQueueTestScope : BaseTestScope
{
    protected virtual bool StartConsumers => true;
    protected virtual bool EnsureOffsets => false;

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);

        hostBuilder.GetSitkoCore()
            .AddModule<KafkaModule, KafkaModuleOptions>(options =>
            {
                options.Brokers =
                    [$"{KafkaFixture.KafkaContainer.Hostname}:{KafkaFixture.KafkaContainer.GetMappedPublicPort(9092)}"];
                options.AutoOffsetReset = AutoOffsetReset.Latest;
                options.EnsureOffsets = EnsureOffsets;
                options.WaitForConsumerAssignments = true;
                options.TopicPartitionsCount = 3;
            })
            .ConfigureLogLevel("KafkaFlow", LogEventLevel.Debug)
            .ConfigureLogLevel("Sitko.Core.Kafka", LogEventLevel.Debug);
        hostBuilder.AddKafkaQueue(options =>
        {
            options.TopicPrefix = Guid.NewGuid().ToString();
            options.GroupPrefix = Guid.NewGuid().ToString();
            options.StartConsumers = StartConsumers;
            options.AddAssembly<BaseKafkaQueueTest>();
        });

        return hostBuilder;
    }

    public override async Task OnCreatedAsync()
    {
        await StartApplicationAsync(TestContext.Current.CancellationToken);
        await base.OnCreatedAsync();
    }
}

public class StoppedKafkaQueueTestScope : BaseKafkaQueueTestScope
{
    protected override bool StartConsumers => false;
    protected override bool EnsureOffsets => true;
}
