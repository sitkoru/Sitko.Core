using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using Sitko.Core.App;

namespace Sitko.Core.Pdf
{
    using System.Text.Json.Serialization;

    public class PdfRendererModule : BaseApplicationModule<PdfRendererModuleOptions>
    {
        public override string OptionsKey => "PdfRenderer";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            PdfRendererModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddTransient<IPdfRenderer, PdfRenderer>();
        }
    }

    public class PdfRendererModuleOptions : BaseModuleOptions
    {
        public string? BrowserWsEndpoint { get; set; }
        public bool IgnoreHTTPSErrors { get; set; } = false;

        [JsonIgnore]
        public ViewPortOptions ViewPortOptions { get; set; } = ViewPortOptions.Default;
    }
}
