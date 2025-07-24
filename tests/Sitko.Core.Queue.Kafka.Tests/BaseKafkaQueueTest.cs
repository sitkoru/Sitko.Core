using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Kafka;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

public abstract class BaseKafkaQueueTest(ITestOutputHelper testOutputHelper)
    : BaseKafkaQueueTest<BaseKafkaQueueTestScope>(testOutputHelper);

public abstract class BaseKafkaQueueTest<T>(ITestOutputHelper testOutputHelper)
    : BaseTest<T>(testOutputHelper) where T : BaseKafkaQueueTestScope;

public class BaseKafkaQueueTestScope : BaseTestScope
{
    private KafkaContainer container = null!;

    protected virtual bool StartConsumers => true;

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);

        hostBuilder.GetSitkoCore().AddModule<KafkaModule, KafkaModuleOptions>(options =>
        {
            options.Brokers = [$"{container.Hostname}:{container.GetMappedPublicPort(9092)}"];
        });
        hostBuilder.AddKafkaQueue(options =>
        {
            options.TopicPrefix = Guid.NewGuid().ToString();
            options.GroupPrefix = Guid.NewGuid().ToString();
            options.AddAssembly<BaseKafkaQueueTest>();
            options.StartConsumers = StartConsumers;
        });

        return hostBuilder;
    }

    public override async Task BeforeConfiguredAsync(string name)
    {
        await base.BeforeConfiguredAsync(name);
        container = new KafkaBuilder().WithImage("confluentinc/cp-kafka:7.4.0").Build();

        await container.StartAsync()
            .ConfigureAwait(false);
    }

    protected override async Task OnAfterDisposeAsync()
    {
        await base.OnAfterDisposeAsync();
        await container.StopAsync(CancellationToken.None);
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
}
