using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.ImgProxy
{
    public class ImgProxyModule : BaseApplicationModule<ImgProxyModuleOptions>
    {
        public override string OptionsKey => "ImgProxy";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            ImgProxyModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IImgProxyUrlGenerator, ImgProxyUrlGenerator>();
        }
    }
}
