using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Sitko.Core.App;

namespace Sitko.Core.Db.Postgres
{
    public class PostgresDatabaseModule<TDbContext> : BaseDbModule<TDbContext, PostgresDatabaseModuleOptions<TDbContext>>
        where TDbContext : DbContext
    {
        public override string GetOptionsKey()
        {
            return $"Db:Postgres:{typeof(TDbContext).Name}";
        }

        public override async Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider)
        {
            await base.InitAsync(context, serviceProvider);
            if (GetOptions(serviceProvider).AutoApplyMigrations)
            {
                var logger = serviceProvider.GetService<ILogger<PostgresDatabaseModule<TDbContext>>>();
                var migrated = false;
                for (var i = 1; i <= 10; i++)
                {
                    logger.LogInformation("Migrate database");
                    try
                    {
                        var dbContext = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TDbContext>();
                        if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
                        {
                            await dbContext.Database.MigrateAsync();
                        }

                        migrated = true;
                        break;
                    }
                    catch (NpgsqlException ex)
                    {
                        logger.LogError(ex, "Error migrating database: {ErrorText}. Try #{TryNumber}", ex.ToString(),
                            i);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
                    }
                }

                if (migrated)
                {
                    logger.LogInformation("Database migrated");
                }
                else
                {
                    throw new Exception("Can't migrate database after 10 tries. See previous errors");
                }
            }
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            PostgresDatabaseModuleOptions<TDbContext> startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddMemoryCache();
            if (startupOptions.EnableContextPooling)
            {
                services.AddDbContextPool<TDbContext>((serviceProvider, options) =>
                    ConfigureNpgsql(options, serviceProvider, context.Configuration, context.Environment));
            }
            else
            {
                services.AddDbContext<TDbContext>((serviceProvider, options) =>
                    ConfigureNpgsql(options, serviceProvider, context.Configuration, context.Environment));
            }
        }

        private NpgsqlConnectionStringBuilder CreateBuilder(
            PostgresDatabaseModuleOptions<TDbContext> options)
        {
            var connBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = options.Host,
                Port = options.Port,
                Username = options.Username,
                Password = options.Password,
                Database = options.Database,
                Pooling = options.EnableNpgsqlPooling
            };
            return connBuilder;
        }

        private void ConfigureNpgsql(DbContextOptionsBuilder options,
            IServiceProvider serviceProvider, IConfiguration configuration, IHostEnvironment environment)
        {
            var config = GetOptions(serviceProvider);
            options.UseNpgsql(CreateBuilder(config).ConnectionString,
                builder => builder.MigrationsAssembly(config.MigrationsAssembly != null
                    ? config.MigrationsAssembly.FullName
                    : typeof(TDbContext).Assembly.FullName));
            if (config.EnableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider, configuration,
                environment);
        }
    }
}
