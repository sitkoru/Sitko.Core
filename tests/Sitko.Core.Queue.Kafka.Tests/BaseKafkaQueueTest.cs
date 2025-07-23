using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Kafka;
using Sitko.Core.Xunit;
using Testcontainers.Kafka;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Kafka.Tests;

public abstract class BaseKafkaQueueTest(ITestOutputHelper testOutputHelper)
    : BaseTest<BaseKafkaQueueTestScope>(testOutputHelper);

public class BaseKafkaQueueTestScope : BaseTestScope
{
    private KafkaContainer container = null!;

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
        await StartApplicationAsync();
        await base.OnCreatedAsync();
    }
}
