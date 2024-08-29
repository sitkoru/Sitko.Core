using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using Sitko.Core.App;
using Sitko.Core.App.Localization;

namespace Sitko.Core.Blazor.MudBlazorComponents;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddMudBlazor(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreBlazorApplicationBuilder>().AddMudBlazor();
        return hostApplicationBuilder;
    }

    public static ISitkoCoreBlazorApplicationBuilder AddMudBlazor(this ISitkoCoreBlazorApplicationBuilder applicationBuilder)
    {
        applicationBuilder.ConfigureServices(services =>
        {
            services.AddMudServices();
            // services.AddScoped<ILayoutManager<MudLayoutData, MudLayoutOptions>, MudLayoutManager>();
            services.Configure<JsonLocalizationModuleOptions>(options =>
            {
                options.AddDefaultResource(typeof(ApplicationExtensions));
            });
        });

        return applicationBuilder;
    }
}
