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
        webApplicationBuilder.AddSitkoCore().AddAntBlazorServer();
        return webApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder
        AddAntBlazorServer(this SitkoCoreApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.AddBlazorServer().AddBlazorFileUpload();
}
