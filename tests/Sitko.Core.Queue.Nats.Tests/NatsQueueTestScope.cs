using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.Nats.Tests
{
    public class NatsQueueTestScope : BaseQueueTestScope<NatsQueueModule, NatsQueue, NatsQueueModuleOptions>
    {
        protected virtual void ConfigureQueue(NatsQueueModuleOptions options, IConfiguration configuration,
            IHostEnvironment environment)
        {
        }

        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            NatsQueueModuleOptions options, string name)
        {
            if (string.IsNullOrEmpty(options.ClusterName))
            {
                options.ClusterName = "tests";
            }

            options.Verbose = true;
            options.ConnectionTimeout = TimeSpan.FromSeconds(5);
            options.QueueNamePrefix = name.Replace(".", "_");
            ConfigureQueue(options, configuration, environment);
        }
    }
}
