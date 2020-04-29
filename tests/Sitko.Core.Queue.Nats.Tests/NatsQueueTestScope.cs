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
            if (!string.IsNullOrEmpty(configuration["QUEUE_NATS_CLUSTER_NAME"]))
            {
                config.ClusterName = configuration["QUEUE_NATS_CLUSTER_NAME"];
            }
            else
            {
                config.ClusterName = "cg2";
            }

            if (!string.IsNullOrEmpty(configuration["QUEUE_NATS_HOST"]))
            {
                config.AddServer(configuration["QUEUE_NATS_HOST"], Convert.ToInt32(configuration["QUEUE_NATS_PORT"]));
            }
            else
            {
                config.AddServer("127.0.0.1", 4222);
            }

            config.Verbose = true;
            config.ConnectionTimeout = TimeSpan.FromSeconds(5);
            config.QueueNamePrefix = name.Replace(".", "_");
            ConfigureQueue(config, configuration, environment);
        }
    }
}
