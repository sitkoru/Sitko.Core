using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ClickHouse;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddClickHouse(
        this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ClickHouseModuleOptions> configure,
        string? optionsKey = null)
    {
        applicationBuilder.GetSitkoCore().AddClickHouse(configure, optionsKey);
        return applicationBuilder;
    }
    public static IHostApplicationBuilder AddClickHouse(
        this IHostApplicationBuilder applicationBuilder,
        Action<ClickHouseModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        applicationBuilder.GetSitkoCore().AddClickHouse(configure, optionsKey);
        return applicationBuilder;
    }
    public static ISitkoCoreApplicationBuilder AddClickHouse(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ClickHouseModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder
            .AddModule<ClickHouseModule, ClickHouseModuleOptions>(configure,
                optionsKey);
    public static ISitkoCoreApplicationBuilder AddClickHouse(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ClickHouseModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder
            .AddModule<ClickHouseModule, ClickHouseModuleOptions>(configure,
                optionsKey);

    // public static IHostApplicationBuilder AddClickHouseDb<TDbContext>(
    //     this IHostApplicationBuilder applicationBuilder,
    //     Action<IApplicationContext, ClickHouseModuleOptions> configure,
    //     string? optionsKey = null)
    //     where TDbContext : BaseClickHouseDbContext
    // {
    //     applicationBuilder.GetSitkoCore().AddClickHouseDb<TDbContext>(configure, optionsKey);
    //     return applicationBuilder;
    // }
    //
    // public static IHostApplicationBuilder AddClickHouseDb<TDbContext>(
    //     this IHostApplicationBuilder applicationBuilder,
    //     Action<ClickHouseModuleOptions>? configure = null,
    //     string? optionsKey = null)
    //     where TDbContext : BaseClickHouseDbContext
    // {
    //     applicationBuilder.GetSitkoCore().AddClickHouseDb<TDbContext>(configure, optionsKey);
    //     return applicationBuilder;
    // }
    //
    // public static ISitkoCoreApplicationBuilder AddClickHouseDb<TDbContext>(
    //     this ISitkoCoreApplicationBuilder applicationBuilder,
    //     Action<IApplicationContext, ClickHouseModuleOptions> configure,
    //     string? optionsKey = null)
    //     where TDbContext : BaseClickHouseDbContext =>
    //     applicationBuilder
    //         .AddModule<ClickHouseModule<TDbContext>, ClickHouseModuleOptions>(configure,
    //             optionsKey);
    //
    // public static ISitkoCoreApplicationBuilder AddClickHouseDb<TDbContext>(
    //     this ISitkoCoreApplicationBuilder applicationBuilder,
    //     Action<ClickHouseModuleOptions>? configure = null,
    //     string? optionsKey = null)
    //     where TDbContext : BaseClickHouseDbContext =>
    //     applicationBuilder
    //         .AddModule<ClickHouseModule<TDbContext>, ClickHouseModuleOptions>(configure,
    //             optionsKey);
}
