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
    using JetBrains.Annotations;

    [PublicAPI]
    public abstract class DbBaseTestScope<TApplication, TDbContext> : BaseTestScope<TApplication>
        where TDbContext : DbContext where TApplication : Application
    {
        private TDbContext? scopeDbContext;

        protected override TApplication ConfigureApplication(TApplication application, string name)
        {
            base.ConfigureApplication(application, name);

            var testInMemory =
                string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("XUNIT_USE_POSTGRES"))
                || !bool.TryParse(System.Environment.GetEnvironmentVariable("XUNIT_USE_POSTGRES"), out var outBool) ||
                !outBool;
            if (testInMemory)
            {
                application.AddInMemoryDatabase<TDbContext>(moduleOptions =>
                {
                    moduleOptions.Database = name;
                    moduleOptions.ConfigureDbContextOptions = (builder, _, conf, env) =>
                    {
                        ConfigureInMemoryDatabaseModule(builder, conf, env);
                    };
                });
            }
            else
            {
                application.AddPostgresDatabase<TDbContext>((configuration, environment, moduleOptions) =>
                {
                    moduleOptions.Database = $"{application.Id}_{name}";
                    moduleOptions.EnableSensitiveLogging = true;
                    moduleOptions.IncludeErrorDetails = true;
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
            scopeDbContext = ServiceProvider!.GetService<TDbContext>();
            if (scopeDbContext == null)
            {
                throw new InvalidOperationException("Can't create db context");
            }

            await scopeDbContext.Database.EnsureDeletedAsync();
            await scopeDbContext.Database.EnsureCreatedAsync();
            await InitDbContextAsync(scopeDbContext);
        }

        public TDbContext GetDbContext()
        {
            if (scopeDbContext == null)
            {
                throw new InvalidOperationException("Db context is null");
            }

            return scopeDbContext;
        }

        protected virtual Task InitDbContextAsync(TDbContext dbContext) => Task.CompletedTask;

        protected virtual void ConfigurePostgresDatabaseModule(IConfiguration configuration,
            IHostEnvironment environment, PostgresDatabaseModuleOptions<TDbContext> moduleOptions, Guid applicationId,
            string dbName)
        {
        }

        public override async ValueTask DisposeAsync()
        {
            if (scopeDbContext != null)
            {
                await scopeDbContext.Database.EnsureDeletedAsync();
            }

            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    public abstract class DbBaseTestScope<TDbContext> : DbBaseTestScope<TestApplication, TDbContext>
        where TDbContext : DbContext
    {
    }
}
