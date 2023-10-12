using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Sitko.Core.App;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Server;

namespace Sitko.Core.Blazor.MudBlazor.Server;

[PublicAPI]
public static class ApplicationExtensions
{
    public static WebApplicationBuilder AddMudBlazorServer(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddSitkoCore().AddMudBlazorServer();
        return webApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder
        AddMudBlazorServer(this SitkoCoreApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.AddBlazorServer().AddMudBlazor();
}
