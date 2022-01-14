using Sitko.Core.App;
using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.Nats.Tests;

public class NatsQueueTestScope : BaseQueueTestScope<NatsQueueModule, NatsQueue, NatsQueueModuleOptions>
{
    protected virtual void ConfigureQueue(NatsQueueModuleOptions options, IApplicationContext applicationContext)
    {
    }

    protected override void Configure(IApplicationContext applicationContext,
        NatsQueueModuleOptions options, string name)
    {
        if (string.IsNullOrEmpty(options.ClusterName))
        {
            options.ClusterName = "tests";
        }

        options.Verbose = true;
        options.ConnectionTimeoutInSeconds = 5;
        options.QueueNamePrefix = name.Replace(".", "_");
        ConfigureQueue(options, applicationContext);
    }
}
