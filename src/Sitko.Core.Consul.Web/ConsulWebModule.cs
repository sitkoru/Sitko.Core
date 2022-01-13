using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;

namespace Sitko.Core.Consul.Web;

public class ConsulWebModule : BaseApplicationModule<ConsulWebModuleOptions>
{
    public override string OptionsKey => "Consul:Web";

    public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
        ConsulWebModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddSingleton<ConsulWebClient>();
        services.AddHealthChecks().AddCheck<ConsulWebHealthCheck>("Consul registration");
    }

    public override Task ApplicationStarted(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<ConsulWebClient>();
        return client.RegisterAsync();
    }

    public override async Task ApplicationStopping(IConfiguration configuration, IAppEnvironment environment,
        IServiceProvider serviceProvider)
    {
        var consulClient = serviceProvider.GetRequiredService<IConsulClientProvider>();
        var logger = serviceProvider.GetRequiredService<ILogger<ConsulWebModule>>();
        logger.LogInformation("Remove service from Consul");
        await consulClient.Client.Agent.ServiceDeregister(environment.ApplicationName);
    }

    public override IEnumerable<Type>
        GetRequiredModules(ApplicationContext context, ConsulWebModuleOptions options) =>
        new[] { typeof(ConsulModule) };
}
