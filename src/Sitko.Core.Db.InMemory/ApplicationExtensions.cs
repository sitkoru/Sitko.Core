using Microsoft.EntityFrameworkCore;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory;

public static class ApplicationExtensions
{
    public static Application AddInMemoryDatabase<TDbContext>(this Application application,
        Action<IApplicationContext, InMemoryDatabaseModuleOptions<TDbContext>> configure,
        string? optionsKey = null)
        where TDbContext : DbContext =>
        application
            .AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleOptions<TDbContext>>(configure,
                optionsKey);

    public static Application AddInMemoryDatabase<TDbContext>(this Application application,
        Action<InMemoryDatabaseModuleOptions<TDbContext>>? configure = null,
        string? optionsKey = null)
        where TDbContext : DbContext =>
        application
            .AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleOptions<TDbContext>>(configure,
                optionsKey);
}

