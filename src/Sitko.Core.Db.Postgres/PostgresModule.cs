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
        public PostgresModule(PostgresDatabaseModuleConfig<TDbContext> config, Application application) : base(config,
            application)
        {
        }

        public override void CheckConfig()
        {
            base.CheckConfig();

            if (string.IsNullOrEmpty(Config.Host))
            {
                throw new ArgumentException("Postgres host is empty");
            }

            if (string.IsNullOrEmpty(Config.Username))
            {
                throw new ArgumentException("Postgres username is empty");
            }

            if (string.IsNullOrEmpty(Config.Database))
            {
                throw new ArgumentException("Postgres database is empty");
            }

            if (Config.Port == 0)
            {
                throw new ArgumentException("Postgres host is empty");
            }
        }

        public override async Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment)
        {
            await base.InitAsync(serviceProvider, configuration, environment);

            if (Config.AutoApplyMigrations)
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

            var connBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = Config.Host,
                Port = Config.Port,
                Username = Config.Username,
                Password = Config.Password,
                Database = Config.Database,
                Pooling = Config.EnableNpgsqlPooling
            };

            services.AddMemoryCache();
            if (Config.EnableContextPooling)
            {
                services.AddDbContextPool<TDbContext>((p, options) =>
                    ConfigureNpgsql(options, connBuilder, p, configuration, environment));
            }
            else
            {
                services.AddDbContext<TDbContext>((p, options) =>
                    ConfigureNpgsql(options, connBuilder, p, configuration, environment));
            }
        }

        private void ConfigureNpgsql(DbContextOptionsBuilder options, NpgsqlConnectionStringBuilder connBuilder,
            IServiceProvider p, IConfiguration configuration, IHostEnvironment environment)
        {
            options.UseNpgsql(connBuilder.ConnectionString,
                builder => builder.MigrationsAssembly(Config.MigrationsAssembly != null
                    ? Config.MigrationsAssembly.FullName
                    : typeof(TDbContext).Assembly.FullName));
            if (Config.EnableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            Config.Configure?.Invoke((DbContextOptionsBuilder<TDbContext>)options, p, configuration, environment);
        }
    }
}
