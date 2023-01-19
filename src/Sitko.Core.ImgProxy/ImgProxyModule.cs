using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.ImgProxy;

public class ImgProxyModule : BaseApplicationModule<ImgProxyModuleOptions>
{
    public override string OptionsKey => "ImgProxy";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ImgProxyModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IImgProxyUrlGenerator, ImgProxyUrlGenerator>();
    }
}

