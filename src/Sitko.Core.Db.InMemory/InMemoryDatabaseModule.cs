using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory;

public class
    InMemoryDatabaseModule<TDbContext> : BaseDbModule<TDbContext, InMemoryDatabaseModuleOptions<TDbContext>>
    where TDbContext : DbContext
{
    public override string OptionsKey => $"Db:InMemory:{typeof(TDbContext).Name}";

    public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
        InMemoryDatabaseModuleOptions<TDbContext> startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        if (startupOptions.EnableContextPooling)
        {
            services.AddDbContextPool<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, context.Configuration, context.Environment));
            services.AddPooledDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, context.Configuration, context.Environment));
        }
        else
        {
            services.AddDbContext<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, context.Configuration, context.Environment));
            services.AddDbContextFactory<TDbContext>((serviceProvider, options) =>
                ConfigureInMemory(options, serviceProvider, context.Configuration, context.Environment));
        }
    }

    private void ConfigureInMemory(DbContextOptionsBuilder options,
        IServiceProvider serviceProvider, IConfiguration configuration, IAppEnvironment environment)
    {
        var config = GetOptions(serviceProvider);
        options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(config.Database);
        config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
            configuration,
            environment);
        if (config.EnableSensitiveLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
            configuration,
            environment);
    }
}
