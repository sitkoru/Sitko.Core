using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;
using Sitko.Core.App.Health;
using Sitko.Core.Consul.ServiceDiscovery;

namespace Sitko.Core.Consul;

public class ConsulModule : BaseApplicationModule<ConsulModuleOptions>
{
    public override string OptionsKey => "Consul";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ConsulModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IConsulClientProvider, ConsulClientProvider>();
        services.AddSingleton<IServiceDiscoveryManager, ServiceDiscoveryManager>();
        services.AddSingleton(provider => provider.GetRequiredService<IConsulClientProvider>().Client);
        services.AddHealthChecks().AddCheck<ConsulHealthCheck>("Consul connection",
            HealthStatus.Unhealthy,
            HealthCheckStages.GetSkipTags(HealthCheckStages.Liveness, HealthCheckStages.Readiness));
    }
}

public class ConsulModuleOptions : BaseModuleOptions
{
    public string ConsulUri { get; set; } = "http://localhost:8500";
    public int ChecksIntervalInSeconds { get; set; } = 60;
    public int DeregisterTimeoutInSeconds { get; set; } = 60;
}

public class ConsulModuleOptionsValidator : AbstractValidator<ConsulModuleOptions>
{
    public ConsulModuleOptionsValidator() =>
        RuleFor(options => options.ConsulUri).NotEmpty().WithMessage("Consul uri can't be empty");
}
