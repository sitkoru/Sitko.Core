using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Sitko.Core.App;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Blazor.Server;

namespace Sitko.Core.Blazor.AntDesign.Server;

[PublicAPI]
public static class ApplicationExtensions
{
    public static WebApplicationBuilder AddAntBlazorServer(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddSitkoCore<ISitkoCoreBlazorServerApplicationBuilder>().AddAntBlazorServer();
        return webApplicationBuilder;
    }

    public static ISitkoCoreBlazorServerApplicationBuilder
        AddAntBlazorServer(this ISitkoCoreBlazorServerApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddBlazorFileUpload();
        return webApplicationBuilder;
    }
}
