using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Db.Postgres;

namespace Sitko.Core.Xunit
{
    public abstract class DbBaseTestScope<TApplication, TScope, TDbContext> : BaseTestScope<TApplication>
        where TScope : class where TDbContext : DbContext where TApplication : Application
    {
        private TDbContext? _dbContext;

        protected override TApplication ConfigureApplication(TApplication application, string name)
        {
            base.ConfigureApplication(application, name);

            var testInMemory =
                string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("XUNIT_USE_POSTGRES"))
                || !bool.TryParse(System.Environment.GetEnvironmentVariable("XUNIT_USE_POSTGRES"), out var outBool) ||
                !outBool;
            if (testInMemory)
            {
                application.AddModule<InMemoryDatabaseModule<TDbContext>, InMemoryDatabaseModuleOptions<TDbContext>>(
                    (_, _, moduleConfig) =>
                    {
                        moduleConfig.Database = name;
                        moduleConfig.Configure = (builder, _, conf, env) =>
                        {
                            ConfigureInMemoryDatabaseModule(builder, conf, env);
                        };
                    });
            }
            else
            {
                application.AddModule<PostgresModule<TDbContext>, PostgresDatabaseModuleOptions<TDbContext>>((
                    configuration,
                    environment, moduleOptions) =>
                {
                    moduleOptions.Database = $"{application.Id}_{name}";
                    ConfigurePostgresDatabaseModule(configuration, environment, moduleOptions, application.Id, name);
                });
            }

            return application;
        }

        protected virtual void ConfigureInMemoryDatabaseModule(DbContextOptionsBuilder builder,
            IConfiguration configuration, IHostEnvironment environment)
        {
        }

        public override async Task OnCreatedAsync()
        {
            _dbContext = ServiceProvider!.GetService<TDbContext>();
            if (_dbContext == null)
            {
                throw new Exception("Can't create db context");
            }

            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.Database.EnsureCreatedAsync();
            await InitDbContextAsync(_dbContext);
        }

        public TDbContext GetDbContext()
        {
            if (_dbContext == null)
            {
                throw new Exception("Db context is null");
            }

            return _dbContext;
        }

        protected virtual Task InitDbContextAsync(TDbContext dbContext)
        {
            return Task.CompletedTask;
        }

        protected virtual void ConfigurePostgresDatabaseModule(IConfiguration configuration,
            IHostEnvironment environment, PostgresDatabaseModuleOptions<TDbContext> moduleOptions, Guid applicationId,
            string dbName)
        {
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

    public abstract class DbBaseTestScope<TScope, TDbContext> : DbBaseTestScope<TestApplication, TScope, TDbContext>
        where TScope : class where TDbContext : DbContext
    {
    }
}
