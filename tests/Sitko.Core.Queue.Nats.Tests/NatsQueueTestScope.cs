using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.Nats.Tests
{
    public class NatsQueueTestScope : BaseQueueTestScope<NatsQueueModule, NatsQueue, NatsQueueModuleConfig>
    {
        protected virtual void ConfigureQueue(NatsQueueModuleConfig config, IConfiguration configuration,
            IHostEnvironment environment)
        {
        }

        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            NatsQueueModuleConfig config, string name)
        {
            if (string.IsNullOrEmpty(config.ClusterName))
            {
                config.ClusterName = "tests";
            }

            config.Verbose = true;
            config.ConnectionTimeout = TimeSpan.FromSeconds(5);
            config.QueueNamePrefix = name.Replace(".", "_");
            ConfigureQueue(config, configuration, environment);
        }
    }
}
