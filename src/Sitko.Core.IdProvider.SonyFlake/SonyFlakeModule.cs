using System;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeModule : BaseApplicationModule<SonyFlakeModuleConfig>
    {
        public override string GetConfigKey()
        {
            return "SonyFlake";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            SonyFlakeModuleConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddHttpClient<IIdProvider, SonyFlakeIdProvider>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(GetConfig(serviceProvider).Uri);
            });
        }
    }
}
