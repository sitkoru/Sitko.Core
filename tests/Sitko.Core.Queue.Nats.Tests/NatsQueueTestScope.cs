using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.Nats.Tests
{
    public class NatsQueueTestScope : BaseQueueTestScope<NatsQueueModule, NatsQueue, NatsQueueModuleConfig>
    {
        protected override NatsQueueModuleConfig CreateConfig(IConfiguration configuration,
            IHostEnvironment environment, string name)
        {
            var config = new NatsQueueModuleConfig(configuration["QUEUE_NATS_CLUSTER_NAME"],
                new List<(string host, int port)>
                {
                    (configuration["QUEUE_NATS_HOST"], Convert.ToInt32(configuration["QUEUE_NATS_PORT"]))
                })
            {
                Verbose = true,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                QueueNamePrefix = name.Replace(".", "_")
            };
            ConfigureQueue(config, configuration, environment);
            return config;
        }

        protected virtual void ConfigureQueue(NatsQueueModuleConfig config, IConfiguration configuration,
            IHostEnvironment environment)
        {
        }
    }
}
