using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Consul;

public class ConsulModule : BaseApplicationModule<ConsulModuleOptions>
{
    public override string OptionsKey => "Consul";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ConsulModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IConsulClientProvider, ConsulClientProvider>();
        services.AddSingleton(provider => provider.GetRequiredService<IConsulClientProvider>().Client);
        services.AddHealthChecks().AddCheck<ConsulHealthCheck>("Consul connection");
    }
}

public class ConsulModuleOptions : BaseModuleOptions
{
    public string ConsulUri { get; set; } = "http://localhost:8500";
}

public class ConsulModuleOptionsValidator : AbstractValidator<ConsulModuleOptions>
{
    public ConsulModuleOptionsValidator() =>
        RuleFor(options => options.ConsulUri).NotEmpty().WithMessage("Consul uri can't be empty");
}

