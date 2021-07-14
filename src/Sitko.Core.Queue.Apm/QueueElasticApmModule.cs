using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.ElasticStack;

namespace Sitko.Core.Queue.Apm
{
    public class QueueElasticApmModule : BaseApplicationModule
    {
        public override string OptionsKey => "Queue:Elastic:Apm";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BaseApplicationModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IQueueMiddleware, QueueElasticApmMiddleware>();
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            BaseApplicationModuleOptions options) =>
            new List<Type>(base.GetRequiredModules(context, options)) {typeof(ElasticStackModule)};
    }
}
