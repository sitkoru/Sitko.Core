using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using Sitko.Core.App.Blazor;
using Sitko.Core.App.Blazor.Layout;
using Sitko.Core.App.Localization;

namespace Sitko.Core.Blazor.MudBlazorComponents
{
    public class MudBlazorStartup : BlazorStartup
    {
        public MudBlazorStartup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
            environment)
        {
        }

        protected override void ConfigureAppServices(IServiceCollection services)
        {
            base.ConfigureAppServices(services);
            services.AddMudServices();
            services.AddScoped<ILayoutManager<MudLayoutData, MudLayoutOptions>, MudLayoutManager>();
            services.Configure<JsonLocalizationModuleOptions>(options =>
            {
                options.AddDefaultResource(typeof(MudBlazorApplication<>));
            });
        }
    }
}
