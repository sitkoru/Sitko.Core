using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Db.Postgres;

namespace Sitko.Core.Xunit;

[PublicAPI]
public abstract class DbBaseTestScope<TApplication, TConfig> : BaseTestScope<TApplication, TConfig>
    where TApplication : HostedApplication where TConfig : BaseDbTestConfig, new()
{
    private readonly List<DbContext> dbContexts = new();

    private readonly List<Func<Task>> dbInitActions = new();

    protected void AddDbContext<TDbContext>(TApplication application, string name, Action<DbContextOptionsBuilder,
            IApplicationContext>? configureInMemory = null,
        Action<IApplicationContext, PostgresDatabaseModuleOptions<TDbContext>, Guid,
            string>? configurePostgres = null, Func<TDbContext, Task>? initDbContext = null)
        where TDbContext : DbContext
    {
        var dbName = $"{typeof(TDbContext).Name}_{name}";
        application.AddInMemoryDatabase<TDbContext>((applicationContext, moduleOptions) =>
        {
            if (GetConfig(applicationContext.Configuration).UsePostgres)
            {
                moduleOptions.Enabled = false;
            }
            else
            {
                moduleOptions.Database = dbName;
                moduleOptions.ConfigureDbContextOptions = (builder, _, currentApplicationContext) =>
                {
                    configureInMemory?.Invoke(builder, currentApplicationContext);
                };
            }
        });
        application.AddPostgresDatabase<TDbContext>((applicationContext, moduleOptions) =>
        {
            if (GetConfig(applicationContext.Configuration).UsePostgres)
            {
                var fullDbName = $"{Id.ToString()[..8]}_{dbName}";
                moduleOptions.Database = fullDbName;
                moduleOptions.EnableSensitiveLogging = true;
                moduleOptions.IncludeErrorDetails = true;
                configurePostgres?.Invoke(applicationContext, moduleOptions, Id, dbName);
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

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        foreach (var dbContext in dbContexts)
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
    }
}

[PublicAPI]
public abstract class DbBaseTestScope<TApplication, TDbContext, TConfig> : DbBaseTestScope<TApplication, TConfig>
    where TDbContext : DbContext where TApplication : HostedApplication where TConfig : BaseDbTestConfig, new()
{
    protected override TApplication ConfigureApplication(TApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        AddDbContext<TDbContext>(application, name, ConfigureInMemoryDatabaseModule, ConfigurePostgresDatabaseModule,
            InitDbContextAsync);
        return application;
    }


    protected virtual void ConfigureInMemoryDatabaseModule(DbContextOptionsBuilder builder,
        IApplicationContext applicationContext)
    {
    }

    public TDbContext GetDbContext() => GetDbContext<TDbContext>();

    protected virtual Task InitDbContextAsync(TDbContext dbContext) => Task.CompletedTask;

    protected virtual void ConfigurePostgresDatabaseModule<TSomeDbContext>(IApplicationContext applicationContext,
        PostgresDatabaseModuleOptions<TSomeDbContext> moduleOptions, Guid applicationId,
        string dbName) where TSomeDbContext : DbContext
    {
    }
}

public abstract class DbBaseTestScope<TDbContext> : DbBaseTestScope<TestApplication, TDbContext, BaseDbTestConfig>
    where TDbContext : DbContext
{
}

