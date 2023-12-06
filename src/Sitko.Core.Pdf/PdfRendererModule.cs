using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Puppeteer;

namespace Sitko.Core.Pdf;

public class PdfRendererModule : BaseApplicationModule
{
    public override string OptionsKey => "PdfRenderer";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        BaseApplicationModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddTransient<IPdfRenderer, PdfRenderer>();
    }

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        BaseApplicationModuleOptions options) => new[] { typeof(PuppeteerModule) };
}
