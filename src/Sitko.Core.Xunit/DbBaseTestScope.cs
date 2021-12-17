using System;
using System.Collections.Generic;
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
public abstract class DbBaseTestScope<TApplication, TConfig> : BaseTestScope<TApplication, TConfig>
    where TApplication : Application where TConfig : BaseDbTestConfig, new()
{
    private readonly List<DbContext> dbContexts = new();

    private readonly List<Func<Task>> dbInitActions = new();

    protected void AddDbContext<TDbContext>(TApplication application, string name, Action<DbContextOptionsBuilder,
        IConfiguration, IHostEnvironment>? configureInMemory = null, Action<IConfiguration,
        IHostEnvironment, PostgresDatabaseModuleOptions<TDbContext>, Guid,
        string>? configurePostgres = null, Func<TDbContext, Task>? initDbContext = null) where TDbContext : DbContext
    {
        var dbName = $"{typeof(TDbContext).Name}_{name}";
        application.AddInMemoryDatabase<TDbContext>((configuration, _, moduleOptions) =>
        {
            if (GetConfig(configuration).UsePostgres)
            {
                moduleOptions.Enabled = false;
            }
            else
            {
                moduleOptions.Database = dbName;
                moduleOptions.ConfigureDbContextOptions = (builder, _, conf, env) =>
                {
                    configureInMemory?.Invoke(builder, conf, env);
                };
            }
        });
        application.AddPostgresDatabase<TDbContext>((configuration, environment, moduleOptions) =>
        {
            if (GetConfig(configuration).UsePostgres)
            {
                moduleOptions.Database = $"{application.Id}_{dbName}";
                moduleOptions.EnableSensitiveLogging = true;
                moduleOptions.IncludeErrorDetails = true;
                configurePostgres?.Invoke(configuration, environment, moduleOptions, application.Id, dbName);
            }
            else
            {
                moduleOptions.Enabled = false;
            }
        });

        dbInitActions.Add(async () =>
        {
            var dbContext = ServiceProvider!.GetService<TDbContext>();
            if (dbContext == null)
            {
                throw new InvalidOperationException("Can't create db context");
            }

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            if (initDbContext is not null)
            {
                await initDbContext(dbContext);
            }

            dbContexts.Add(dbContext);
        });
    }

    public override async Task OnCreatedAsync()
    {
        await base.OnCreatedAsync();
        foreach (var dbInitAction in dbInitActions)
        {
            await dbInitAction();
        }
    }

    public TDbContext GetDbContext<TDbContext>() where TDbContext : DbContext =>
        (TDbContext)(dbContexts.Find(dbContext => dbContext is TDbContext) ??
                     throw new InvalidOperationException("Db context is null"));

    public override async ValueTask DisposeAsync()
    {
        foreach (var dbContext in dbContexts)
        {
            await dbContext.Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

[PublicAPI]
public abstract class DbBaseTestScope<TApplication, TDbContext, TConfig> : DbBaseTestScope<TApplication, TConfig>
    where TDbContext : DbContext where TApplication : Application where TConfig : BaseDbTestConfig, new()
{
    protected override TApplication ConfigureApplication(TApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        AddDbContext<TDbContext>(application, name, ConfigureInMemoryDatabaseModule, ConfigurePostgresDatabaseModule,
            InitDbContextAsync);
        return application;
    }


    protected virtual void ConfigureInMemoryDatabaseModule(DbContextOptionsBuilder builder,
        IConfiguration configuration, IHostEnvironment environment)
    {
    }

    public TDbContext GetDbContext() => GetDbContext<TDbContext>();

    protected virtual Task InitDbContextAsync(TDbContext dbContext) => Task.CompletedTask;

    protected virtual void ConfigurePostgresDatabaseModule<TSomeDbContext>(IConfiguration configuration,
        IHostEnvironment environment, PostgresDatabaseModuleOptions<TSomeDbContext> moduleOptions, Guid applicationId,
        string dbName) where TSomeDbContext : DbContext
    {
    }
}

public abstract class DbBaseTestScope<TDbContext> : DbBaseTestScope<TestApplication, TDbContext, BaseDbTestConfig>
    where TDbContext : DbContext
{
}
