using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Blazor;
using Sitko.Core.App.Localization;

namespace Sitko.Core.Blazor.AntDesignComponents
{
    public class AntBlazorStartup : BlazorStartup
    {
        public AntBlazorStartup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
            environment)
        {
        }

        protected override void ConfigureAppServices(IServiceCollection services)
        {
            base.ConfigureAppServices(services);
            services.AddAntDesign();
            services.Configure<JsonLocalizationModuleOptions>(options =>
            {
                options.AddDefaultResource(typeof(AntBlazorApplication<>));
            });
        }
    }
}
