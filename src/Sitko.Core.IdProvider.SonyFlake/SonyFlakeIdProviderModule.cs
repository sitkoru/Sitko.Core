using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake;

public class SonyFlakeIdProviderModule : BaseIdProviderModule<SonyFlakeIdProvider, SonyFlakeIdProviderModuleOptions>
{
    public override string OptionsKey => "IdProvider:SonyFlake";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        SonyFlakeIdProviderModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddHttpClient();
    }
}

