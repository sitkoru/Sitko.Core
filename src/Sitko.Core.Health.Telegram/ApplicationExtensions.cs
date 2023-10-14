using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddTelegramHealthReporter(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, TelegramHealthReporterModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddTelegramHealthReporter(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddTelegramHealthReporter(this IHostApplicationBuilder hostApplicationBuilder,
        Action<TelegramHealthReporterModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddTelegramHealthReporter(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddTelegramHealthReporter(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, TelegramHealthReporterModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<TelegramHealthReporterModule, TelegramHealthReporterModuleOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddTelegramHealthReporter(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<TelegramHealthReporterModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<TelegramHealthReporterModule, TelegramHealthReporterModuleOptions>(configure,
            optionsKey);
}
