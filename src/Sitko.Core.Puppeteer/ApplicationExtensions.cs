﻿using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Puppeteer;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddPuppeteer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, PuppeteerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddPuppeteer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddPuppeteer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<PuppeteerModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddPuppeteer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddPuppeteer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, PuppeteerModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<PuppeteerModule, PuppeteerModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddPuppeteer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<PuppeteerModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<PuppeteerModule, PuppeteerModuleOptions>(configure, optionsKey);
}
