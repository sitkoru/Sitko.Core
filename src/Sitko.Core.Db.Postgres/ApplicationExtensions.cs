using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db.Postgres;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddPostgresDatabase<TDbContext>(
        this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, PostgresDatabaseModuleOptions<TDbContext>> configure,
        string? optionsKey = null)
        where TDbContext : DbContext
    {
        applicationBuilder.GetSitkoCore().AddPostgresDatabase(configure, optionsKey);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder AddPostgresDatabase<TDbContext>(
        this IHostApplicationBuilder applicationBuilder,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configure = null,
        string? optionsKey = null)
        where TDbContext : DbContext
    {
        applicationBuilder.GetSitkoCore().AddPostgresDatabase(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddPostgresDatabase<TDbContext>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, PostgresDatabaseModuleOptions<TDbContext>> configure,
        string? optionsKey = null)
        where TDbContext : DbContext =>
        applicationBuilder
            .AddModule<PostgresDatabaseModule<TDbContext>, PostgresDatabaseModuleOptions<TDbContext>>(configure,
                optionsKey);

    public static ISitkoCoreApplicationBuilder AddPostgresDatabase<TDbContext>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configure = null,
        string? optionsKey = null)
        where TDbContext : DbContext =>
        applicationBuilder
            .AddModule<PostgresDatabaseModule<TDbContext>, PostgresDatabaseModuleOptions<TDbContext>>(configure,
                optionsKey);
}
