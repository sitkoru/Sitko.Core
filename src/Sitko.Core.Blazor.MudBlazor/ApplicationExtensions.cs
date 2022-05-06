using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Sitko.Core.App;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.Layout;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public static class ApplicationExtensions
{
    public static Application AddMudBlazor(this Application application) =>
        application.ConfigureServices(services =>
        {
            services.AddMudServices();
            services.AddScoped<ILayoutManager<MudLayoutData, MudLayoutOptions>, MudLayoutManager>();
            services.Configure<JsonLocalizationModuleOptions>(options =>
            {
                options.AddDefaultResource(typeof(ApplicationExtensions));
            });
        });
}
