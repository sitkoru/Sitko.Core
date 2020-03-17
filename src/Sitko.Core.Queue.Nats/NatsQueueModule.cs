using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Nats
{
    public class NatsQueueModule : QueueModule<NatsQueue, NatsQueueModuleConfig>
    {
        public NatsQueueModule(NatsQueueModuleConfig config, Application application) : base(config, application)
        {
        }

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
        public string ClusterName { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string? ConsumerGroupName { get; set; }
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool Verbose { get; set; }

        public string? QueueNamePrefix { get; set; }

        public NatsQueueModuleConfig AddServer(string host, int port)
        {
            Servers.Add((host, port));
            return this;
        }
    }
}
