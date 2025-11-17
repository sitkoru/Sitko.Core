using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ClickHouse.Linq2db;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddClickHouseDb<TDbContext>(
        this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ClickHouseLinq2dbModuleOptions> configure,
        string? optionsKey = null)
        where TDbContext : BaseClickHouseDbContext
    {
        applicationBuilder.GetSitkoCore().AddClickHouseDb<TDbContext>(configure, optionsKey);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder AddClickHouseDb<TDbContext>(
        this IHostApplicationBuilder applicationBuilder,
        Action<ClickHouseLinq2dbModuleOptions>? configure = null,
        string? optionsKey = null)
        where TDbContext : BaseClickHouseDbContext
    {
        applicationBuilder.GetSitkoCore().AddClickHouseDb<TDbContext>(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddClickHouseDb<TDbContext>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ClickHouseLinq2dbModuleOptions> configure,
        string? optionsKey = null)
        where TDbContext : BaseClickHouseDbContext =>
        applicationBuilder
            .AddModule<ClickHouseLinq2dbModule<TDbContext>, ClickHouseLinq2dbModuleOptions>(configure,
                optionsKey);

    public static ISitkoCoreApplicationBuilder AddClickHouseDb<TDbContext>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ClickHouseLinq2dbModuleOptions>? configure = null,
        string? optionsKey = null)
        where TDbContext : BaseClickHouseDbContext =>
        applicationBuilder
            .AddModule<ClickHouseLinq2dbModule<TDbContext>, ClickHouseLinq2dbModuleOptions>(configure,
                optionsKey);
}
