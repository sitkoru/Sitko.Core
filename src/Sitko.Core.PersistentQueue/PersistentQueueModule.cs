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
    public abstract class
        PersistentQueueModule<T, TOptions, TConnection, TConnectionFactory> : BaseApplicationModule<TOptions>
        where TOptions : PersistentQueueModuleOptions
        where TConnection : IPersistentQueueConnection
        where TConnectionFactory : class, IPersistentQueueConnectionFactory<TConnection>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<PersistentQueueMetricsCollector>();

            services.AddSingleton<IPersistentQueueConnectionFactory<TConnection>, TConnectionFactory>();

            var consumerType = GetConsumerType();
            if (!typeof(IPersistentQueueConsumer).IsAssignableFrom(consumerType))
            {
                throw new Exception("Consumer type must implement IPersistentQueueConsumer");
            }

            services.AddTransient(typeof(IPersistentQueueConsumer<>), consumerType);

            var producerType = GetProducerType();
            if (!typeof(IPersistentQueueProducer).IsAssignableFrom(producerType))
            {
                throw new Exception("Producer type must implement IPersistentQueueProducer");
            }

            services.AddTransient(typeof(IPersistentQueueProducer<>), producerType);

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
            
            services.AddHealthChecks().AddCheck<PersistentQueueHealthCheck<TConnection>>("Nats Persistent Queue Health check");
        }

        protected abstract Type GetConsumerType();
        protected abstract Type GetProducerType();

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(MetricsModule)};
        }
    }
}
