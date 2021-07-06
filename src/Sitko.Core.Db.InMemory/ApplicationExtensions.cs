using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory
{
    public static class ApplicationExtensions
    {
        public static Application AddInMemoryDatabase<TDbContext>(this Application application,
            Action<IConfiguration, IHostEnvironment, InMemoryDatabaseModuleOptions<TDbContext>> configure,
            string? optionsKey = null)
            where TDbContext : DbContext
        {
            return application
                .AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleOptions<TDbContext>>(configure,
                    optionsKey);
        }

        public static Application AddInMemoryDatabase<TDbContext>(this Application application,
            Action<InMemoryDatabaseModuleOptions<TDbContext>>? configure = null,
            string? optionsKey = null)
            where TDbContext : DbContext
        {
            return application
                .AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleOptions<TDbContext>>(configure,
                    optionsKey);
        }
    }
}
