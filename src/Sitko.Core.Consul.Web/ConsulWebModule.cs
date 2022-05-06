using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;

namespace Sitko.Core.Consul.Web;

public class ConsulWebModule : BaseApplicationModule<ConsulWebModuleOptions>
{
    public override string OptionsKey => "Consul:Web";

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        ConsulWebModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddSingleton<ConsulWebClient>();
        services.AddHealthChecks().AddCheck<ConsulWebHealthCheck>("Consul registration");
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
        GetRequiredModules(IApplicationContext context, ConsulWebModuleOptions options) =>
        new[] { typeof(ConsulModule) };
}
