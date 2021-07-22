using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.MediatR;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue
{
    public interface IQueueModule : IApplicationModule
    {
    }

    public abstract class QueueModule<TQueue, TConfig> : BaseApplicationModule<TConfig>, IQueueModule
        where TQueue : class, IQueue
        where TConfig : QueueModuleOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IQueue, TQueue>();
            services.AddSingleton<QueueContext>();

            if (startupOptions.HealthChecksEnabled)
            {
                services.AddHealthChecks().AddCheck<QueueHealthCheck>("Queue health check");
            }

            if (startupOptions.Middlewares.Any())
            {
                services.Scan(selector =>
                    selector.AddTypes(startupOptions.Middlewares).AsSelfWithInterfaces().WithSingletonLifetime());
            }

            foreach (var options in startupOptions.Options)
            {
                services.AddSingleton(typeof(IQueueMessageOptions), options.Value);
            }

            if (startupOptions.ProcessorEntries.Any())
            {
                var types = startupOptions.ProcessorEntries.Select(e => e.Type).Distinct().ToArray();
                services.Scan(selector => selector.AddTypes(types).AsSelfWithInterfaces().WithScopedLifetime());
                var messageTypes = startupOptions.ProcessorEntries.SelectMany(e => e.MessageTypes).Distinct().ToArray();
                foreach (var messageType in messageTypes)
                {
                    var host = typeof(QueueProcessorHost<>).MakeGenericType(messageType);
                    services.AddSingleton(typeof(IHostedService), host);
                }
            }

            foreach ((Type serviceType, Type implementationType) in startupOptions.TranslateMediatRTypes)
            {
                services.AddTransient(serviceType, implementationType);
            }
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context, TConfig options)
        {
            var modules = new List<Type>(base.GetRequiredModules(context, options));

            if (options.TranslateMediatRTypes.Any())
            {
                modules.Add(typeof(IMediatRModule));
            }

            return modules;
        }
    }
}
