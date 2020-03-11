using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Sitko.Core.App;

namespace Sitko.Core.Db.Postgres
{
    public class PostgresModule<TDbContext> : BaseDbModule<TDbContext, PostgresDatabaseModuleConfig<TDbContext>>
        where TDbContext : DbContext
    {
        public PostgresModule(PostgresDatabaseModuleConfig<TDbContext> config, Application application) : base(config, application)
        {
        }

        protected override void CheckConfig()
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

            if (environment.IsProduction())
            {
                var dbContext = serviceProvider.GetRequiredService<TDbContext>();
                if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
                {
                    await dbContext.Database.MigrateAsync();
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

            Config.Configure?.Invoke(options as DbContextOptionsBuilder<TDbContext>, p, configuration, environment);
        }
    }
}
