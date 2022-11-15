﻿using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Puppeteer;

public class PuppeteerModule : BaseApplicationModule<PuppeteerModuleOptions>
{
    public override string OptionsKey => "Puppeteer";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        PuppeteerModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IBrowserProvider, BrowserProvider>();
    }
}

