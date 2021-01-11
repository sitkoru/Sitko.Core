using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Pdf
{
    public class PdfRendererModule : BaseApplicationModule<PdfRendererModuleConfig>
    {
        public PdfRendererModule(PdfRendererModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IPdfRenderer, PdfRenderer>();
        }
    }

    public class PdfRendererModuleConfig
    {
        public bool IgnoreHTTPSErrors { get; set; } = false;
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
    }
}
