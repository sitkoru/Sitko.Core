using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.MessageBus;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue
{
    public abstract class QueueModule<TQueue, TConfig> : BaseApplicationModule<TConfig>
        where TQueue : class, IQueue
        where TConfig : QueueModuleConfig
    {
        protected QueueModule(TConfig config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IQueue, TQueue>();
            services.AddSingleton<QueueContext>();

            if (Config.HealthChecksEnabled)
            {
                services.AddHealthChecks().AddCheck<QueueHealthCheck>("Queue health check");
            }

            if (Config.Middlewares.Any())
            {
                services.Scan(selector =>
                    selector.AddTypes(Config.Middlewares).AsSelfWithInterfaces().WithSingletonLifetime());
            }

            foreach (var options in Config.Options)
            {
                services.AddSingleton(typeof(IQueueMessageOptions), options.Value);
            }

            if (Config.ProcessorEntries.Any())
            {
                var types = Config.ProcessorEntries.Select(e => e.Type).Distinct().ToArray();
                services.Scan(selector => selector.AddTypes(types).AsSelfWithInterfaces().WithScopedLifetime());
                var messageTypes = Config.ProcessorEntries.SelectMany(e => e.MessageTypes).Distinct().ToArray();
                foreach (var messageType in messageTypes)
                {
                    var host = typeof(QueueProcessorHost<>).MakeGenericType(messageType);
                    services.AddSingleton(typeof(IHostedService), host);
                }
            }

            foreach ((Type serviceType, Type implementationType) in Config.TranslateMessageBusTypes)
            {
                services.AddTransient(serviceType, implementationType);
            }
        }

        public override List<Type> GetRequiredModules()
        {
            var modules = new List<Type>();

            if (Config.TranslateMessageBusTypes.Any())
            {
                modules.Add(typeof(MessageBusModule));
            }

            return modules;
        }
    }
}
