using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Db.InMemory;
using Sitko.Core.Db.Postgres;

namespace Sitko.Core.Xunit;

[PublicAPI]
public abstract class DbBaseTestScope<TApplicationBuilder, TConfig> : BaseTestScope<TApplicationBuilder, TConfig>
    where TConfig : BaseDbTestConfig, new() where TApplicationBuilder : IHostApplicationBuilder
{
    private readonly List<DbContext> dbContexts = new();

    private readonly List<Func<Task>> dbInitActions = new();

    protected void AddDbContext<TDbContext>(IHostApplicationBuilder applicationBuilder, string name,
        Action<DbContextOptionsBuilder,
            IApplicationContext>? configureInMemory = null,
        Action<IApplicationContext, PostgresDatabaseModuleOptions<TDbContext>, Guid,
            string>? configurePostgres = null, Func<TDbContext, Task>? initDbContext = null)
        where TDbContext : DbContext
    {
        var dbName = $"{typeof(TDbContext).Name}_{name}";
        applicationBuilder
            .AddInMemoryDatabase<TDbContext>((applicationContext, moduleOptions) =>
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
            })
            .AddPostgresDatabase<TDbContext>((applicationContext, moduleOptions) =>
            {
                if (GetConfig(applicationContext.Configuration).UsePostgres)
                {
                    var fullDbName = $"{Id.ToString()[..8]}_{dbName}";
                    moduleOptions.Database = fullDbName;
                    moduleOptions.EnableSensitiveLogging = true;
                    moduleOptions.IncludeErrorDetails = true;
                    moduleOptions.EnableNpgsqlPooling = false;
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
public abstract class
    DbBaseTestScope<TApplicationBuilder, TDbContext, TConfig> : DbBaseTestScope<TApplicationBuilder, TConfig>
    where TDbContext : DbContext
    where TConfig : BaseDbTestConfig, new()
    where TApplicationBuilder : IHostApplicationBuilder
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        AddDbContext<TDbContext>(hostBuilder, name, ConfigureInMemoryDatabaseModule, ConfigurePostgresDatabaseModule,
            InitDbContextAsync);
        return hostBuilder;
    }

    protected virtual void ConfigureInMemoryDatabaseModule(DbContextOptionsBuilder builder,
        IApplicationContext applicationContext)
    {
    }

    public TDbContext GetDbContext() => GetDbContext<TDbContext>();

    protected virtual Task InitDbContextAsync(TDbContext dbContext) => Task.CompletedTask;

    protected virtual void ConfigurePostgresDatabaseModule<TSomeDbContext>(IApplicationContext applicationContext,
        PostgresDatabaseModuleOptions<TSomeDbContext> moduleOptions, Guid applicationId,
        string dbName) where TSomeDbContext : DbContext =>
        moduleOptions.ConfigureDbContextOptions = (builder, _, _) =>
        {
            builder.ConfigureWarnings(configurationBuilder =>
                configurationBuilder.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        };
}

public abstract class
    DbBaseTestScope<TDbContext> : DbBaseTestScope<HostApplicationBuilder, TDbContext, BaseDbTestConfig>
    where TDbContext : DbContext
{
    protected override IHost BuildApplication(HostApplicationBuilder builder) => builder.Build();
    protected override HostApplicationBuilder CreateHostBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddSitkoCore();
        return builder;
    }
}
