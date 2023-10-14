using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Pdf;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddPdfRenderer(this IHostApplicationBuilder hostApplicationBuilder
    )
    {
        hostApplicationBuilder.GetSitkoCore().AddPdfRenderer();
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddPdfRenderer(this ISitkoCoreApplicationBuilder applicationBuilder) =>
        applicationBuilder.AddModule<PdfRendererModule>();
}
