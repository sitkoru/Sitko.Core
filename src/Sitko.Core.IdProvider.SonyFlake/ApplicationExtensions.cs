using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddSonyFlakeIdProvider(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, SonyFlakeIdProviderModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSonyFlakeIdProvider(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddSonyFlakeIdProvider(this IHostApplicationBuilder hostApplicationBuilder,
        Action<SonyFlakeIdProviderModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSonyFlakeIdProvider(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddSonyFlakeIdProvider(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, SonyFlakeIdProviderModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<SonyFlakeIdProviderModule, SonyFlakeIdProviderModuleOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddSonyFlakeIdProvider(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<SonyFlakeIdProviderModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<SonyFlakeIdProviderModule, SonyFlakeIdProviderModuleOptions>(configure,
            optionsKey);
}
