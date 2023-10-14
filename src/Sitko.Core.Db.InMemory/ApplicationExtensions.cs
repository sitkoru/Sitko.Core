using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddInMemoryDatabase<TDbContext>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, InMemoryDatabaseModuleOptions<TDbContext>> configure,
        string? optionsKey = null)
        where TDbContext : DbContext
    {
        hostApplicationBuilder.AddSitkoCore().AddInMemoryDatabase(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddInMemoryDatabase<TDbContext>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<InMemoryDatabaseModuleOptions<TDbContext>>? configure = null,
        string? optionsKey = null)
        where TDbContext : DbContext
    {
        hostApplicationBuilder.AddSitkoCore().AddInMemoryDatabase(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddInMemoryDatabase<TDbContext>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, InMemoryDatabaseModuleOptions<TDbContext>> configure,
        string? optionsKey = null)
        where TDbContext : DbContext =>
        applicationBuilder
            .AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleOptions<TDbContext>>(configure,
                optionsKey);

    public static ISitkoCoreApplicationBuilder AddInMemoryDatabase<TDbContext>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<InMemoryDatabaseModuleOptions<TDbContext>>? configure = null,
        string? optionsKey = null)
        where TDbContext : DbContext =>
        applicationBuilder
            .AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleOptions<TDbContext>>(configure,
                optionsKey);
}
