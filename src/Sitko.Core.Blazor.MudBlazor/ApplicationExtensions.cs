using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using Sitko.Core.App;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.Layout;

namespace Sitko.Core.Blazor.MudBlazorComponents;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddMudBlazor(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.AddSitkoCore().AddMudBlazor();
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddMudBlazor(this SitkoCoreApplicationBuilder applicationBuilder) =>
        applicationBuilder.ConfigureServices(services =>
        {
            services.AddMudServices();
            services.AddScoped<ILayoutManager<MudLayoutData, MudLayoutOptions>, MudLayoutManager>();
            services.Configure<JsonLocalizationModuleOptions>(options =>
            {
                options.AddDefaultResource(typeof(ApplicationExtensions));
            });
        });
}
