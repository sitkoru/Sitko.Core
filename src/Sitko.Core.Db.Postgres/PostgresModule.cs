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
    public class PostgresModule<TDbContext> : BaseDbModule<TDbContext, PostgresDatabaseModuleConfig<TDbContext>>
        where TDbContext : DbContext
    {
        public override string GetConfigKey()
        {
            return $"Db:Postgres:{typeof(TDbContext).Name}";
        }

        public override async Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider)
        {
            await base.InitAsync(context, serviceProvider);
            if (GetConfig(serviceProvider).AutoApplyMigrations)
            {
                var logger = serviceProvider.GetService<ILogger<PostgresModule<TDbContext>>>();
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
            PostgresDatabaseModuleConfig<TDbContext> startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddMemoryCache();
            if (startupConfig.EnableContextPooling)
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
            PostgresDatabaseModuleConfig<TDbContext> config)
        {
            var connBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = config.Host,
                Port = config.Port,
                Username = config.Username,
                Password = config.Password,
                Database = config.Database,
                Pooling = config.EnableNpgsqlPooling
            };
            return connBuilder;
        }

        private void ConfigureNpgsql(DbContextOptionsBuilder options,
            IServiceProvider serviceProvider, IConfiguration configuration, IHostEnvironment environment)
        {
            var config = GetConfig(serviceProvider);
            options.UseNpgsql(CreateBuilder(config).ConnectionString,
                builder => builder.MigrationsAssembly(config.MigrationsAssembly != null
                    ? config.MigrationsAssembly.FullName
                    : typeof(TDbContext).Assembly.FullName));
            if (config.EnableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            config.Configure?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider, configuration,
                environment);
        }
    }
}
