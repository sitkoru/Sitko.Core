using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;
using Sitko.Core.App.Health;

namespace Sitko.Core.Grpc.Server.Discovery;

public abstract class DiscoveryGrpcServerModule<TRegistrar, TConfig> : BaseGrpcServerModule<TConfig>
    where TRegistrar : class, IGrpcServicesRegistrar where TConfig : GrpcServerModuleOptions, new()
{
    private readonly List<Action<IHealthChecksBuilder>> healthChecksRegistrations = new();

    private readonly List<Func<IGrpcServicesRegistrar, CancellationToken, Task>> serviceRegistrations = new();

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IGrpcServicesRegistrar, TRegistrar>();
        var healthChecksBuilder = services.AddHealthChecks();
        foreach (var healthChecksRegistration in healthChecksRegistrations)
        {
            healthChecksRegistration(healthChecksBuilder);
        }
    }

    public override async Task ApplicationStarted(IApplicationContext applicationContext,
        IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var registrar = serviceProvider.GetRequiredService<IGrpcServicesRegistrar>();
        foreach (var serviceRegistration in serviceRegistrations)
        {
            await serviceRegistration(registrar, cancellationToken);
        }
    }

    public override void RegisterService<TService>(string? requiredAuthorizationSchemeName, bool enableGrpcWeb = false)
    {
        base.RegisterService<TService>(requiredAuthorizationSchemeName, enableGrpcWeb);
        serviceRegistrations.Add((registrar, cancellationToken) =>
            registrar.RegisterAsync<TService>(cancellationToken));
        healthChecksRegistrations.Add(healthCheckBuilder =>
            healthCheckBuilder.AddCheck<GrpcServiceHealthCheck<TService>>(
                $"Grpc service {typeof(TService).BaseType?.DeclaringType?.Name}",
                HealthStatus.Degraded,
                tags: HealthCheckStages.GetSkipAllTags()));
    }
}
