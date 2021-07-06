using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Pdf
{
    public static class ApplicationExtensions
    {
        public static Application AddPdfRenderer(this Application application,
            Action<IConfiguration, IHostEnvironment, PdfRendererModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<PdfRendererModule, PdfRendererModuleOptions>(configure, optionsKey);
        }

        public static Application AddPdfRenderer(this Application application,
            Action<PdfRendererModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<PdfRendererModule, PdfRendererModuleOptions>(configure, optionsKey);
        }
    }
}
