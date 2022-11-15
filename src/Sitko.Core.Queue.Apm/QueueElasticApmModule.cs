using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.ElasticStack;

namespace Sitko.Core.Queue.Apm;

public class QueueElasticApmModule : BaseApplicationModule
{
    public override string OptionsKey => "Queue:Elastic:Apm";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        BaseApplicationModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IQueueMiddleware, QueueElasticApmMiddleware>();
    }

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        BaseApplicationModuleOptions options) =>
        new List<Type>(base.GetRequiredModules(applicationContext, options)) { typeof(ElasticStackModule) };
}

