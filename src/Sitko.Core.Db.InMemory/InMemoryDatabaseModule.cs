using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory
{
    public class
        InMemoryDatabaseModule<TDbContext> : BaseDbModule<TDbContext, InMemoryDatabaseModuleOptions<TDbContext>>
        where TDbContext : DbContext
    {
        public override string GetOptionsKey()
        {
            return $"Db:InMemory:{typeof(TDbContext).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            InMemoryDatabaseModuleOptions<TDbContext> startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddDbContext<TDbContext>((serviceProvider, options) =>
            {
                var config = GetOptions(serviceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .UseInMemoryDatabase(config.Database);
                config.ConfigureDbContextOptions?.Invoke((DbContextOptionsBuilder<TDbContext>)options, serviceProvider,
                    context.Configuration,
                    context.Environment);
            });
        }
    }
}
