using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuppeteerSharp;
using Sitko.Core.App;

namespace Sitko.Core.Pdf
{
    public class PdfRendererModule : BaseApplicationModule<PdfRendererModuleConfig>
    {
        public PdfRendererModule(Application application) : base(application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IPdfRenderer, PdfRenderer>();
        }
    }

    public class PdfRendererModuleConfig : BaseModuleConfig
    {
        public bool IgnoreHTTPSErrors { get; set; } = false;
        public ViewPortOptions ViewPortOptions { get; set; } = ViewPortOptions.Default;
    }
}
