using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider)
    {
        await base.InitAsync(applicationContext, serviceProvider);
        var logger = serviceProvider.GetRequiredService<ILogger<PdfRendererModule>>();
        logger.LogCritical("PDF INIT");
    }
}
