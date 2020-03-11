using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.ElasticStack;

namespace Sitko.Core.Queue.Apm
{
    public class QueueElasticApmModule : BaseApplicationModule
    {
        public QueueElasticApmModule(BaseApplicationModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IQueueMiddleware, QueueElasticApmMiddleware>();
        }

        public override List<Type> GetRequiredModules()
        {
            var list = base.GetRequiredModules();
            list.Add(typeof(ElasticStackModule));
            return list;
        }
    }
}
