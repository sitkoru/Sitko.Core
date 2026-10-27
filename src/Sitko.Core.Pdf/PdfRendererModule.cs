using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Pdf;

public class PdfRendererModule : BaseApplicationModule<PdfRendererModuleOptions>
{
    public override string OptionsKey => "PdfRenderer";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        PdfRendererModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        _ = services.AddTransient<IPdfRenderer, PdfRenderer>();
    }
}
