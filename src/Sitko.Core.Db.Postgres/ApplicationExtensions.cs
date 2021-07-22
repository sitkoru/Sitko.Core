using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db.Postgres
{
    public static class ApplicationExtensions
    {
        public static Application AddPostgresDatabase<TDbContext>(this Application application,
            Action<IConfiguration, IHostEnvironment, PostgresDatabaseModuleOptions<TDbContext>> configure,
            string? optionsKey = null)
            where TDbContext : DbContext =>
            application
                .AddModule<PostgresDatabaseModule<TDbContext>, PostgresDatabaseModuleOptions<TDbContext>>(configure,
                    optionsKey);

        public static Application AddPostgresDatabase<TDbContext>(this Application application,
            Action<PostgresDatabaseModuleOptions<TDbContext>>? configure = null,
            string? optionsKey = null)
            where TDbContext : DbContext =>
            application
                .AddModule<PostgresDatabaseModule<TDbContext>, PostgresDatabaseModuleOptions<TDbContext>>(configure,
                    optionsKey);
    }
}
