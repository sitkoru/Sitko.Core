using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using Sitko.Core.App;

namespace Sitko.Core.Pdf
{
    public class PdfRendererModule : BaseApplicationModule<PdfRendererModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return "PdfRenderer";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            PdfRendererModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddTransient<IPdfRenderer, PdfRenderer>();
        }
    }

    public class PdfRendererModuleOptions : BaseModuleOptions
    {
        public bool IgnoreHTTPSErrors { get; set; } = false;
        public ViewPortOptions ViewPortOptions { get; set; } = ViewPortOptions.Default;
    }
}
