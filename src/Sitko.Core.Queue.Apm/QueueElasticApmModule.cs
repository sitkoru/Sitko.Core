using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.ElasticStack;

namespace Sitko.Core.Queue.Apm
{
    public class QueueElasticApmModule : BaseApplicationModule
    {
        public override string GetConfigKey()
        {
            return "Queue:Elastic:Apm";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BaseApplicationModuleConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddSingleton<IQueueMiddleware, QueueElasticApmMiddleware>();
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            BaseApplicationModuleConfig config)
        {
            return new List<Type>(base.GetRequiredModules(context, config)) {typeof(ElasticStackModule)};
        }
    }
}
