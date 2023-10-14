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
        webApplicationBuilder.AddSitkoCore<ISitkoCoreBlazorServerApplicationBuilder>().AddMudBlazorServer();
        return webApplicationBuilder;
    }

    public static ISitkoCoreBlazorApplicationBuilder
        AddMudBlazorServer(this ISitkoCoreBlazorServerApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.AddMudBlazor();
}
