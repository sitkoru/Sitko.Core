using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory
{
    public class InMemoryDatabaseModule<TDbContext> : BaseDbModule<TDbContext, InMemoryDatabaseModuleConfig<TDbContext>>
        where TDbContext : DbContext
    {
        public override string GetConfigKey()
        {
            return $"Db:InMemory:{typeof(TDbContext).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            InMemoryDatabaseModuleConfig<TDbContext> startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddDbContext<TDbContext>((serviceProvider, options) =>
            {
                var config = GetConfig(serviceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .UseInMemoryDatabase(config.Database);
                config.Configure?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
                    context.Configuration,
                    context.Environment);
            });
        }
    }
}
