using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Puppeteer;

namespace Sitko.Core.Pdf;

public class PdfRendererModule : BaseApplicationModule
{
    public override string OptionsKey => "PdfRenderer";

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        BaseApplicationModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddTransient<IPdfRenderer, PdfRenderer>();
    }

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext context,
        BaseApplicationModuleOptions options) => new[] { typeof(PuppeteerModule) };
}
