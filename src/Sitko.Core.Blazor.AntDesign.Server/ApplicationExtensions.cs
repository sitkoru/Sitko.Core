using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Blazor.Forms;
using Sitko.Core.Blazor.Server;

namespace Sitko.Core.Blazor.AntDesign.Server;

[PublicAPI]
public static class ApplicationExtensions
{
    public static WebApplicationBuilder AddAntBlazorServer(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.GetSitkoCore<ISitkoCoreBlazorServerApplicationBuilder>().AddAntBlazorServer();
        return webApplicationBuilder;
    }

    public static ISitkoCoreBlazorServerApplicationBuilder
        AddAntBlazorServer(this ISitkoCoreBlazorServerApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddBlazorFileUpload();
        webApplicationBuilder.ConfigureServices(collection =>
        {
            collection.AddAntDesign();
            collection.Configure<JsonLocalizationModuleOptions>(options =>
            {
                options.AddDefaultResource(typeof(BaseForm));
            });
        });

        return webApplicationBuilder;
    }
}
