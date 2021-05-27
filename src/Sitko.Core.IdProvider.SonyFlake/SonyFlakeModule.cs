using System;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeModule : BaseApplicationModule<SonyFlakeModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return "SonyFlake";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            SonyFlakeModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddHttpClient<IIdProvider, SonyFlakeIdProvider>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(GetOptions(serviceProvider).Uri);
            });
        }
    }
}
