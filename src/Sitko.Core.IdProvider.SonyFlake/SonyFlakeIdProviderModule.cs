using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeIdProviderModule : BaseIdProviderModule<SonyFlakeIdProvider, SonyFlakeIdProviderModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return "IdProvider:SonyFlake";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            SonyFlakeIdProviderModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddHttpClient();
        }
    }
}
