using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            if (Config.PoolConnections)
            {
                services.AddSingleton<IPersistentQueueConnectionFactory, PooledPersistentQueueConnectionFactory>();
            }
            else
            {
                services.AddSingleton<IPersistentQueueConnectionFactory, SinglePersistentQueueConnectionFactory>();
            }

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
        
        public static List<string> GetNatsAddresses(string host, int port)
        {
            var list = new List<string>();

            if (IPAddress.TryParse(host, out var ip))
            {
                list.Add($"nats://{ip}:{port}");
            }
            else
            {
                var entry = Dns.GetHostEntry(host);
                if (entry.AddressList.Any())
                {
                    foreach (var ipAddress in entry.AddressList)
                    {
                        list.Add($"nats://{ipAddress}:{port}");
                    }
                }
                else
                {
                    throw new Exception($"Can't resolve ip for host {host}");
                }
            }

            return list;
        }
    }
}
