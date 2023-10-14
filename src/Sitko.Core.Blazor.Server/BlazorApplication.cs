using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Blazor.Server;

public interface ISitkoCoreBlazorServerApplicationBuilder : ISitkoCoreBlazorApplicationBuilder, ISitkoCoreServerApplicationBuilder
{
}

public class SitkoCoreBlazorServerApplicationBuilder : SitkoCoreWebApplicationBuilder,
    ISitkoCoreBlazorServerApplicationBuilder
{
    public SitkoCoreBlazorServerApplicationBuilder(WebApplicationBuilder builder, string[] args) : base(builder, args)
    {
        this.AddPersistentState();
        builder.Services.AddScriptInjector();
    }
}

[PublicAPI]
public static class ApplicationExtensions
{
    public static ISitkoCoreBlazorServerApplicationBuilder AddBlazorServer(
        this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.AddBlazorServer(Array.Empty<string>());

    public static ISitkoCoreBlazorServerApplicationBuilder AddBlazorServer(
        this WebApplicationBuilder webApplicationBuilder, string[] args) =>
        ApplicationBuilderFactory.GetOrCreateApplicationBuilder(webApplicationBuilder,
            applicationBuilder => new SitkoCoreBlazorServerApplicationBuilder(applicationBuilder, args));
}
