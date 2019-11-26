using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.PersistentQueue.Nats
{
    public class
        NatsPersistentQueueModule<TAssembly> : PersistentQueueModule<TAssembly, NatsPersistentQueueModuleOptions>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IPersistentQueueConnectionFactory<NatsQueueConnection>, NatsConnectionFactory>();
            services.AddTransient(typeof(IPersistentQueueConsumer<>), typeof(NatsQueueConsumer<>));
            services.AddTransient(typeof(IPersistentQueueProducer<>), typeof(NatsQueueProducer<>));
            services.AddHealthChecks().AddCheck<PersistentQueueHealthCheck>("Nats Persistent Queue Health check");
        }
    }

    public class NatsPersistentQueueModuleOptions : PersistentQueueModuleOptions
    {
        public readonly List<(string host, int port)> Servers = new List<(string host, int port)>();
        public string ClusterName { get; set; }
        public string ClientName { get; set; }
    }
}
