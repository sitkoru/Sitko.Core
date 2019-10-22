using System;
using System.Collections.Generic;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Metrics;
using Sitko.Core.PersistentQueue.Common;
using Sitko.Core.PersistentQueue.Consumer;
using Sitko.Core.PersistentQueue.HostedService;
using Sitko.Core.PersistentQueue.Internal;
using Sitko.Core.PersistentQueue.Producer;

namespace Sitko.Core.PersistentQueue
{
    public class PersistentQueueModule<T> : BaseApplicationModule<PersistentQueueOptions>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<PersistentQueueMetricsCollector>();
            services.AddSingleton<IPersistentQueueConnectionFactory, SinglePersistentQueueConnectionFactory>();

            services.AddSingleton<PersistentQueueConsumerFactory>();
            services.AddSingleton<PersistentQueueProducerFactory>();
            services.AddSingleton(typeof(IPersistentQueueProducer<>), typeof(PersistentQueueProducer<>));

            if (!Config.EmulationMode)
            {
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

            services.AddHealthChecks().AddCheck<PersistentQueueHealthCheck>("Persistent Queue Health check");
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(MetricsModule)};
        }
    }
}
