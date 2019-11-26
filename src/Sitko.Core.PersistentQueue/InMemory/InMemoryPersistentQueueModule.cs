using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.PersistentQueue.InMemory
{
    public class InMemoryPersistentQueueModule<T> : PersistentQueueModule<T, InMemoryPersistentQueueModuleOptions>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services
                .AddSingleton<IPersistentQueueConnectionFactory<InMemoryQueueConnection>, InMemoryConnectionFactory>();
            services.AddTransient(typeof(IPersistentQueueConsumer<>), typeof(InMemoryQueueConsumer<>));
            services.AddTransient(typeof(IPersistentQueueProducer<>), typeof(InMemoryQueueProducer<>));
        }
    }

    public class InMemoryPersistentQueueModuleOptions : PersistentQueueModuleOptions
    {
    }
}
