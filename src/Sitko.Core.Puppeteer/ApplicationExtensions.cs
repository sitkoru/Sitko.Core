using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Puppeteer;

public static class ApplicationExtensions
{
    public static Application AddPuppeteer(this Application application,
        Action<IConfiguration, IHostEnvironment, PuppeteerModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<PuppeteerModule, PuppeteerModuleOptions>(configure, optionsKey);

    public static Application AddPuppeteer(this Application application,
        Action<PuppeteerModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<PuppeteerModule, PuppeteerModuleOptions>(configure, optionsKey);
}
