using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Sitko.Core.App.Localization
{
    public abstract class LocalizationModule<TModuleOptions, TFactory> : BaseApplicationModule<TModuleOptions>
        where TModuleOptions : LocalizationModuleOptions, new()
        where TFactory : class, IStringLocalizerFactory
    {
        public override string GetOptionsKey()
        {
            return "Localization";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.TryAddSingleton<IStringLocalizerFactory, TFactory>();
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        }
    }
    
    public class LocalizationModuleOptions : BaseModuleOptions
    {
    }
}
