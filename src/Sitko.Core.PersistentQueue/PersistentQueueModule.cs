using System;
using System.Collections.Generic;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Metrics;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue
{
    public abstract class PersistentQueueModule<T, TOptions> : BaseApplicationModule<TOptions>
        where TOptions : PersistentQueueModuleOptions
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<PersistentQueueMetricsCollector>();
            var assembly = typeof(T).Assembly;
            foreach (var type in assembly.DefinedTypes)
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                foreach (var implementedInterface in type.ImplementedInterfaces)
                {
                    if (typeof(IPersistentQueueMessageProcessor).IsAssignableFrom(implementedInterface) &&
                        implementedInterface.IsGenericType)
                    {
                        var typeParam = implementedInterface.GetGenericArguments()[0];
                        if (typeof(IMessage).IsAssignableFrom(typeParam))
                        {
                            var genericType =
                                typeof(IPersistentQueueMessageProcessor<>).MakeGenericType(typeParam);
                            var serviceGenericType =
                                typeof(PersistentQueueHostedService<>).MakeGenericType(typeParam);
                            services.AddScoped(genericType, type);
                            services.AddSingleton(typeof(IHostedService), serviceGenericType);
                        }
                    }
                }
            }
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(MetricsModule)};
        }
    }
}
