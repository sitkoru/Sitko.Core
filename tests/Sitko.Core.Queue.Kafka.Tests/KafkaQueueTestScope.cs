using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

public class KafkaQueueTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.AddKafkaQueue();
        return hostBuilder;
    }
}
