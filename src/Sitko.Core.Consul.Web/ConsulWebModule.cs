using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Sitko.Core.App.Health;
using Sitko.Core.App.Web;

namespace Sitko.Core.Consul.Web;

public class ConsulWebModule : BaseApplicationModule<ConsulWebModuleOptions>
{
    public override string OptionsKey => "Consul:Web";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ConsulWebModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<ConsulWebClient>();
        services.AddHealthChecks().AddCheck<ConsulWebHealthCheck>("Consul registration", tags: HealthCheckStages.GetSkipAllTags());
    }

    public override Task ApplicationStarted(IApplicationContext applicationContext,
        IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<ConsulWebClient>();
        return client.RegisterAsync();
    }

    public override async Task ApplicationStopping(IApplicationContext applicationContext,
        IServiceProvider serviceProvider)
    {
        var consulClient = serviceProvider.GetRequiredService<IConsulClientProvider>();
        var logger = serviceProvider.GetRequiredService<ILogger<ConsulWebModule>>();
        logger.LogInformation("Remove service from Consul");
        await consulClient.Client.Agent.ServiceDeregister(applicationContext.Name);
    }

    public override IEnumerable<Type>
        GetRequiredModules(IApplicationContext applicationContext, ConsulWebModuleOptions options) =>
        new[] { typeof(ConsulModule) };
}

