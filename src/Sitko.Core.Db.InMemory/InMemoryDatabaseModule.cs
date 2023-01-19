using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory;

public class
    InMemoryDatabaseModule<TDbContext> : BaseDbModule<TDbContext, InMemoryDatabaseModuleOptions<TDbContext>>
    where TDbContext : DbContext
{
    public override string OptionsKey => $"Db:InMemory:{typeof(TDbContext).Name}";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        InMemoryDatabaseModuleOptions<TDbContext> startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (startupOptions.EnableContextPooling)
        {
            services.AddDbContextPool<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, applicationContext));
            services.AddPooledDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, applicationContext));
        }
        else
        {
            services.AddDbContext<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, applicationContext));
            services.AddDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, applicationContext));
        }
    }

    private void ConfigureInMemory(DbContextOptionsBuilder options,
        IServiceProvider serviceProvider, IApplicationContext applicationContext)
    {
        var config = GetOptions(serviceProvider);
        options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(config.Database);
        config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
            applicationContext);
        if (config.EnableSensitiveLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
            applicationContext);
    }
}

