using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.Queue.Nats
{
    public class NatsQueueModule : QueueModule<NatsQueue, NatsQueueModuleConfig>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            Config.ClientName = environment.ApplicationName.Replace('.', '_');
            if (string.IsNullOrEmpty(Config.ConsumerGroupName))
            {
                Config.ConsumerGroupName = environment.ApplicationName;
            }

            base.ConfigureServices(services, configuration, environment);
        }
    }

    public class NatsQueueModuleConfig : QueueModuleConfig
    {
        public readonly List<(string host, int port)> Servers = new List<(string host, int port)>();

        public NatsQueueModuleConfig(string clusterName, IEnumerable<(string host, int port)> servers)
        {
            ClusterName = clusterName;
            Servers.AddRange(servers);
        }

        public string ClusterName { get; }
        public string ClientName { get; set; }
        public string ConsumerGroupName { get; set; }
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool Verbose { get; set; }

        public string? QueueNamePrefix { get; set; }
    }
}
