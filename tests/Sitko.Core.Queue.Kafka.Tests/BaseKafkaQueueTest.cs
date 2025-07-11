using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Kafka.Tests;

public abstract class BaseKafkaQueueTest(ITestOutputHelper testOutputHelper)
    : BaseTest<BaseKafkaQueueTestScope>(testOutputHelper);

public class BaseKafkaQueueTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);

        hostBuilder.AddKafkaQueue(options =>
        {
            options.TopicPrefix = Guid.NewGuid().ToString();
            options.GroupPrefix = Guid.NewGuid().ToString();
            options.AddAssembly<BaseKafkaQueueTest>();
        });

        return hostBuilder;
    }

    public override async Task OnCreatedAsync()
    {
        await StartApplicationAsync();
        await base.OnCreatedAsync();
    }
}
