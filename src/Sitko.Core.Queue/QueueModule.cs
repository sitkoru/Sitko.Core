using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Health;
using Sitko.Core.MediatR;
using Sitko.Core.Queue.Internal;

namespace Sitko.Core.Queue;

public interface IQueueModule : IApplicationModule;

public abstract class QueueModule<TQueue, TConfig> : BaseApplicationModule<TConfig>, IQueueModule
    where TQueue : class, IQueue
    where TConfig : QueueModuleOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IQueue, TQueue>();
        services.AddSingleton<QueueContext>();

        if (startupOptions.HealthChecksEnabled)
        {
            services.AddHealthChecks().AddCheck<QueueHealthCheck>("Queue health check",
                HealthStatus.Unhealthy,
                HealthCheckStages.GetSkipTags(HealthCheckStages.Liveness, HealthCheckStages.Readiness));
        }

        if (startupOptions.Middlewares.Any())
        {
            services.Scan(selector =>
                selector.FromTypes(startupOptions.Middlewares).AsSelfWithInterfaces().WithSingletonLifetime());
        }

        foreach (var options in startupOptions.Options)
        {
            services.AddSingleton(typeof(IQueueMessageOptions), options.Value);
        }

        if (startupOptions.ProcessorEntries.Any())
        {
            var types = startupOptions.ProcessorEntries.Select(e => e.Type).Distinct().ToArray();
            services.Scan(selector => selector.FromTypes(types).AsSelfWithInterfaces().WithScopedLifetime());
            var messageTypes = startupOptions.ProcessorEntries.SelectMany(e => e.MessageTypes).Distinct().ToArray();
            foreach (var messageType in messageTypes)
            {
                var host = typeof(QueueProcessorHost<>).MakeGenericType(messageType);
                services.AddSingleton(typeof(IHostedService), host);
            }
        }

        foreach (var (serviceType, implementationType) in startupOptions.TranslateMediatRTypes)
        {
            services.AddTransient(serviceType, implementationType);
        }
    }

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext, TConfig options)
    {
        var modules = new List<Type>(base.GetRequiredModules(applicationContext, options));

        if (options.TranslateMediatRTypes.Any())
        {
            modules.Add(typeof(IMediatRModule));
        }

        return modules;
    }
}
