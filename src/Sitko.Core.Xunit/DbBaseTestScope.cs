using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Db.Postgres;

namespace Sitko.Core.Xunit
{
    public abstract class DbBaseTestScope<TScope, TDbContext> : BaseTestScope
        where TScope : class where TDbContext : DbContext
    {
        private TDbContext? _dbContext;

        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name);
            application.ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddEnvironmentVariables()
                    .AddUserSecrets<TScope>();
            });
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddUserSecrets<TScope>()
                .Build();

            var testInMemory =
                string.IsNullOrEmpty(config["CG_TESTS_POSTGRES"])
                || !bool.TryParse(config["CG_TESTS_POSTGRES"], out var outBool) || !outBool;
            if (testInMemory)
            {
                application.AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleConfig<TDbContext>>(
                    (configuration, environment) => new InMemoryDatabaseModuleConfig<TDbContext>(name)
                    {
                        Configure = (builder, provider, conf, env) =>
                        {
                            ConfigureInMemoryDatabaseModule(builder, conf, env);
                        }
                    });
            }
            else
            {
                application.AddModule<PostgresModule<TDbContext>, PostgresDatabaseModuleConfig<TDbContext>>((
                    configuration,
                    environment) =>
                {
                    var postgresConfig = GetPostgresConfig(configuration, environment, name);
                    if (postgresConfig == null)
                    {
                        throw new Exception("Empty postgres config");
                    }

                    return postgresConfig;
                });
            }

            return application;
        }

        protected virtual void ConfigureInMemoryDatabaseModule(DbContextOptionsBuilder builder,
            IConfiguration configuration, IHostEnvironment environment)
        {
        }

        public override void OnCreated()
        {
            _dbContext = ServiceProvider.GetService<TDbContext>();
            if (_dbContext == null)
            {
                throw new Exception("Can't create db context");
            }

            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
            InitDbContext(_dbContext);
        }

        public TDbContext GetDbContext()
        {
            if (_dbContext == null)
            {
                throw new Exception("Db context is null");
            }

            return _dbContext;
        }

        protected virtual void InitDbContext(TDbContext dbContext)
        {
        }

        protected virtual PostgresDatabaseModuleConfig<TDbContext>? GetPostgresConfig(IConfiguration configuration,
            IHostEnvironment environment, string dbName)
        {
            return null;
        }

        public override async ValueTask DisposeAsync()
        {
            if (_dbContext != null)
            {
                await _dbContext.Database.EnsureDeletedAsync();
            }

            await base.DisposeAsync();
        }
    }
}
