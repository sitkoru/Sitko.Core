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
        public PostgresModule(Application application) : base(application)
        {
        }
        
        public override string GetConfigKey()
        {
            return $"Db:Postgres:{typeof(TDbContext).Name}";
        }

        public override async Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment)
        {
            await base.InitAsync(serviceProvider, configuration, environment);

            if (GetConfig().AutoApplyMigrations)
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

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);

            services.AddMemoryCache();
            services.AddDbContextPool<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, configuration, environment));
        }

        private NpgsqlConnectionStringBuilder CreateBuilder()
        {
            var connBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = GetConfig().Host,
                Port = GetConfig().Port,
                Username = GetConfig().Username,
                Password = GetConfig().Password,
                Database = GetConfig().Database,
                Pooling = GetConfig().EnableNpgsqlPooling
            };
            return connBuilder;
        }

        private void ConfigureNpgsql(DbContextOptionsBuilder options,
            IServiceProvider p, IConfiguration configuration, IHostEnvironment environment)
        {
            var config = GetConfig();
            options.UseNpgsql(CreateBuilder().ConnectionString,
                builder => builder.MigrationsAssembly(config.MigrationsAssembly != null
                    ? config.MigrationsAssembly.FullName
                    : typeof(TDbContext).Assembly.FullName));
            if (config.EnableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            config.Configure?.Invoke((DbContextOptionsBuilder<TDbContext>)options, p, configuration, environment);
        }
    }
}
