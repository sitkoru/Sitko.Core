using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using Sitko.Core.App;
using Sitko.Core.App.OpenTelemetry;

namespace Sitko.Core.Db.Postgres;

public class
    PostgresDatabaseModule<TDbContext> : BaseDbModule<TDbContext, PostgresDatabaseModuleOptions<TDbContext>>,
    IOpenTelemetryModule<PostgresDatabaseModuleOptions<TDbContext>>
    where TDbContext : DbContext
{
    public override string OptionsKey => $"Db:Postgres:{typeof(TDbContext).Name}";

    public override string[] OptionKeys => new[] { "Db:Postgres:Default", OptionsKey };

    public OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context,
        PostgresDatabaseModuleOptions<TDbContext> options,
        OpenTelemetryBuilder builder) =>
        builder.WithTracing(providerBuilder => providerBuilder.AddNpgsql());

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await base.InitAsync(applicationContext, serviceProvider, cancellationToken);
        var options = GetOptions(serviceProvider);
        if (options.AutoApplyMigrations)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<PostgresDatabaseModule<TDbContext>>>();
            var migrated = false;
            for (var i = 1; i <= options.MaxMigrationsApplyTryCount; i++)
            {
                logger.LogInformation("Migrate database {Database}. Try #{Try}", options.Database, i);
                try
                {
                    var dbContext = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<TDbContext>();
                    if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                    {
                        await dbContext.Database.MigrateAsync(cancellationToken);
                    }

                    migrated = true;
                    break;
                }
                catch (NpgsqlException ex)
                {
                    logger.LogError(ex, "Error migrating database {Database}: {ErrorText}. Try #{TryNumber}",
                        options.Database, ex.ToString(),
                        i);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)), cancellationToken);
                }
            }

            if (migrated)
            {
                logger.LogInformation("Database {Database} migrated", options.Database);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Can't migrate database after {options.MaxMigrationsApplyTryCount} tries. See previous errors");
            }
        }
    }

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        PostgresDatabaseModuleOptions<TDbContext> startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddMemoryCache();
        if (startupOptions.EnableContextPooling)
        {
            services.AddDbContextPool<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, applicationContext));
            services.AddPooledDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, applicationContext));
        }
        else
        {
            services.AddDbContext<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, applicationContext));
            services.AddDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, applicationContext), startupOptions.DbContextFactoryLifetime);
        }
    }

    private void ConfigureNpgsql(DbContextOptionsBuilder options,
        IServiceProvider serviceProvider, IApplicationContext applicationContext)
    {
        var config = GetOptions(serviceProvider);
        var schemaExtensions = new SchemaDbContextOptionsExtension(config.Schema);
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(config.CreateBuilder().ConnectionString);
        if (config.EnableJsonConversion)
        {
            dataSourceBuilder.EnableDynamicJson();
        }

        var dataSource = dataSourceBuilder.Build();
        options.UseNpgsql(dataSource, builder =>
        {
            builder.MigrationsAssembly(config.MigrationsAssembly != null
                ? config.MigrationsAssembly.FullName
                : typeof(TDbContext).Assembly.FullName);
            if (schemaExtensions.IsCustomSchema)
            {
                builder.MigrationsHistoryTable("__EFMigrationsHistory", schemaExtensions.Schema);
            }
        });
        if (config.EnableSensitiveLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        options.AddExtension(schemaExtensions);

        config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
            applicationContext);
    }
}
