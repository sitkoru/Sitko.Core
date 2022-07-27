using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Sitko.Core.App;

namespace Sitko.Core.Db.Postgres;

public class
    PostgresDatabaseModule<TDbContext> : BaseDbModule<TDbContext, PostgresDatabaseModuleOptions<TDbContext>>
    where TDbContext : DbContext
{
    public override string OptionsKey => $"Db:Postgres:{typeof(TDbContext).Name}";

    public override async Task InitAsync(IApplicationContext context, IServiceProvider serviceProvider)
    {
        await base.InitAsync(context, serviceProvider);
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
                    if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
                    {
                        await dbContext.Database.MigrateAsync();
                    }

                    migrated = true;
                    break;
                }
                catch (NpgsqlException ex)
                {
                    logger.LogError(ex, "Error migrating database {Database}: {ErrorText}. Try #{TryNumber}",
                        options.Database, ex.ToString(),
                        i);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
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

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        PostgresDatabaseModuleOptions<TDbContext> startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddMemoryCache();
        if (startupOptions.EnableContextPooling)
        {
            services.AddDbContextPool<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, context));
            services.AddPooledDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, context));
        }
        else
        {
            services.AddDbContext<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, context));
            services.AddDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureNpgsql(options, serviceProvider, context), startupOptions.DbContextFactoryLifetime);
        }
    }

    private static NpgsqlConnectionStringBuilder CreateBuilder(
        PostgresDatabaseModuleOptions<TDbContext> options)
    {
        var connBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = options.Host,
            Port = options.Port,
            Username = options.Username,
            Password = options.Password,
            Database = options.Database,
            Pooling = options.EnableNpgsqlPooling,
#if NET6_0_OR_GREATER
            IncludeErrorDetail = options.IncludeErrorDetails
#else
                IncludeErrorDetails = options.IncludeErrorDetails
#endif
        };
        foreach (var (key, value) in options.ConnectionOptions)
        {
            try
            {
                connBuilder[key] = value;
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    $"Can't set connection parameter {key} with value {value} for DbContext {typeof(TDbContext)}: {exception.Message}. Check options.");
            }
        }

        return connBuilder;
    }

    private void ConfigureNpgsql(DbContextOptionsBuilder options,
        IServiceProvider serviceProvider, IApplicationContext applicationContext)
    {
        var config = GetOptions(serviceProvider);
        options.UseNpgsql(CreateBuilder(config).ConnectionString,
            builder => builder.MigrationsAssembly(config.MigrationsAssembly != null
                ? config.MigrationsAssembly.FullName
                : typeof(TDbContext).Assembly.FullName));
        if (config.EnableSensitiveLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
            applicationContext);
    }
}
