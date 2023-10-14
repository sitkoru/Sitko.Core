using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Sitko.Core.App;

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
}