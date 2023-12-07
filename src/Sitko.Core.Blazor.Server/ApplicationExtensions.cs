using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Blazor.Server;

[PublicAPI]
public static class ApplicationExtensions
{
    public static ISitkoCoreBlazorServerApplicationBuilder AddSitkoCoreBlazorServer(
        this WebApplicationBuilder webApplicationBuilder) =>
        webApplicationBuilder.AddSitkoCoreBlazorServer(Array.Empty<string>());

    public static ISitkoCoreBlazorServerApplicationBuilder AddSitkoCoreBlazorServer(
        this WebApplicationBuilder webApplicationBuilder, string[] args) =>
        ApplicationBuilderFactory.GetOrCreateApplicationBuilder(webApplicationBuilder,
            applicationBuilder => new SitkoCoreBlazorServerApplicationBuilder(applicationBuilder, args));

    public static ISitkoCoreBlazorApplicationBuilder AddInteractiveWebAssembly(
        this ISitkoCoreBlazorApplicationBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
        });
        return builder;
    }

    public static ISitkoCoreBlazorApplicationBuilder
        AddWebAssemblyAuth<TAuthenticationStateProvider>(this ISitkoCoreBlazorApplicationBuilder builder)
        where TAuthenticationStateProvider : AuthenticationStateProvider
    {
        builder.ConfigureServices(services =>
        {
            services.AddCascadingAuthenticationState();
            services.AddScoped<AuthenticationStateProvider, TAuthenticationStateProvider>();
        });
        return builder;
    }

    public static RazorComponentsEndpointConventionBuilder MapSitkoCoreBlazor<TRootComponent>(this WebApplication app)
    {
        app.MapSitkoCore();
        return app.MapRazorComponents<TRootComponent>()
            .AddInteractiveServerRenderMode();
    }
}
