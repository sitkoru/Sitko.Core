using Sitko.Core.App;

namespace Sitko.Core.Pdf;

public static class ApplicationExtensions
{
    public static Application AddPdfRenderer(this Application application) =>
        application.AddModule<PdfRendererModule>();
}
