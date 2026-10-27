using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Pdf;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddPdfRenderer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, PdfRendererModuleOptions> configure,
        string? optionsKey = null)
    {
        _ = hostApplicationBuilder.GetSitkoCore().AddPdfRenderer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddPdfRenderer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<PdfRendererModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        _ = hostApplicationBuilder.GetSitkoCore().AddPdfRenderer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddPdfRenderer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, PdfRendererModuleOptions> configure,
        string? optionsKey = null) => applicationBuilder
        .AddModule<PdfRendererModule, PdfRendererModuleOptions>(
            configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddPdfRenderer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<PdfRendererModuleOptions>? configure = null,
        string? optionsKey = null) => applicationBuilder
        .AddModule<PdfRendererModule, PdfRendererModuleOptions>(
            configure, optionsKey);
}
