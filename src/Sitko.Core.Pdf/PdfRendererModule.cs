using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using Sitko.Core.App;

namespace Sitko.Core.Pdf
{
    public class PdfRendererModule : BaseApplicationModule<PdfRendererModuleConfig>
    {
        public override string GetConfigKey()
        {
            return "PdfRenderer";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            PdfRendererModuleConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddSingleton<IPdfRenderer, PdfRenderer>();
        }
    }

    public class PdfRendererModuleConfig : BaseModuleConfig
    {
        public bool IgnoreHTTPSErrors { get; set; } = false;
        public ViewPortOptions ViewPortOptions { get; set; } = ViewPortOptions.Default;
    }
}
