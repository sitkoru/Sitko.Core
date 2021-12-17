using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Db.Postgres;

namespace Sitko.Core.Xunit;

[PublicAPI]
public abstract class DbBaseTestScope<TApplication, TDbContext, TConfig> : BaseTestScope<TApplication, TConfig>
    where TDbContext : DbContext where TApplication : Application where TConfig : BaseDbTestConfig, new()
{
    private TDbContext? scopeDbContext;

    protected override TApplication ConfigureApplication(TApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddInMemoryDatabase<TDbContext>((configuration, _, moduleOptions) =>
        {
            if (GetConfig(configuration).UsePostgres)
            {
                moduleOptions.Enabled = false;
            }
            else
            {
                moduleOptions.Database = name;
                moduleOptions.ConfigureDbContextOptions = (builder, _, conf, env) =>
                {
                    ConfigureInMemoryDatabaseModule(builder, conf, env);
                };
            }
        });
        application.AddPostgresDatabase<TDbContext>((configuration, environment, moduleOptions) =>
        {
            if (GetConfig(configuration).UsePostgres)
            {
                moduleOptions.Database = $"{application.Id}_{name}";
                moduleOptions.EnableSensitiveLogging = true;
                moduleOptions.IncludeErrorDetails = true;
                ConfigurePostgresDatabaseModule(configuration, environment, moduleOptions, application.Id, name);
            }
            else
            {
                moduleOptions.Enabled = false;
            }
        });

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

public abstract class DbBaseTestScope<TDbContext> : DbBaseTestScope<TestApplication, TDbContext, BaseDbTestConfig>
    where TDbContext : DbContext
{
}

public abstract class
    DbBaseTestScope<TApplication, TDbContext> : DbBaseTestScope<TApplication, TDbContext, BaseDbTestConfig>
    where TDbContext : DbContext where TApplication : Application
{
}
