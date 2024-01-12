using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App;

public class SitkoCoreServerApplicationBuilder : SitkoCoreBaseApplicationBuilder, ISitkoCoreServerApplicationBuilder
{
    private readonly IHostApplicationBuilder applicationBuilder;

    public SitkoCoreServerApplicationBuilder(IHostApplicationBuilder applicationBuilder, string[] args) : base(args,
        applicationBuilder.Services, applicationBuilder.Configuration,
        new ServerApplicationEnvironment(applicationBuilder.Environment),
        applicationBuilder.Logging)
    {
        this.applicationBuilder = applicationBuilder;
        applicationBuilder.ConfigureContainer(new SitkoCoreServiceProviderBuilderFactory(),
            _ => BeforeContainerBuild());
    }

    protected override IConfigurationBuilder SetupConfiguration(IConfigurationBuilder configurationBuilder)
    {
        base.SetupConfiguration(configurationBuilder);
        configurationBuilder.AddJsonFile("appsettings.local.json", true, false);
        return configurationBuilder;
    }

    protected override void BeforeModuleRegistration<TModule, TModuleOptions>(IApplicationContext applicationContext,
        ApplicationModuleRegistration moduleRegistration)
    {
        base.BeforeModuleRegistration<TModule, TModuleOptions>(applicationContext, moduleRegistration);
        if (typeof(TModule).IsAssignableTo(typeof(IHostBuilderModule)))
        {
            ConfigureHostBuilder<TModule, TModuleOptions>(moduleRegistration);
        }
    }

    protected virtual void ConfigureHostBuilder<TModule, TModuleOptions>(ApplicationModuleRegistration registration)
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new() =>
        registration.ConfigureHostBuilder(BootApplicationContext, applicationBuilder);

    protected override void AfterModuleRegistration<TModule, TModuleOptions>(IApplicationContext applicationContext,
        ApplicationModuleRegistration moduleRegistration)
    {
        base.AfterModuleRegistration<TModule, TModuleOptions>(applicationContext, moduleRegistration);
        if (typeof(TModule).IsAssignableTo(typeof(IHostBuilderModule)))
        {
            moduleRegistration.PostConfigureHostBuilder(BootApplicationContext, applicationBuilder);
        }
    }
}

public class SitkoCoreServiceProviderBuilderFactory : IServiceProviderFactory<SitkoCoreServiceProviderBuilder>
{
    public SitkoCoreServiceProviderBuilder CreateBuilder(IServiceCollection services) => new(services);

    public IServiceProvider CreateServiceProvider(SitkoCoreServiceProviderBuilder containerBuilder) =>
        containerBuilder.Build();
}

public class SitkoCoreServiceProviderBuilder
{
    private readonly IServiceCollection services;

    public SitkoCoreServiceProviderBuilder(IServiceCollection services) => this.services = services;

    public IServiceProvider Build() => services.BuildServiceProvider();
}
