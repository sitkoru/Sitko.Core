using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Blazor.Server;

[PublicAPI]
public static class ApplicationExtensions
{
    public static WebApplicationBuilder AddBlazorServer(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddSitkoCore().AddBlazorServer();
        return webApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddBlazorServer(this SitkoCoreApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.AddPersistentState();
        webApplicationBuilder.ConfigureServices(collection =>
        {
            collection.AddScriptInjector();
        });
        return webApplicationBuilder;
    }
}
