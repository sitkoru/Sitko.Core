using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory
{
    public class InMemoryDatabaseModule<TDbContext> : BaseDbModule<TDbContext, InMemoryDatabaseModuleConfig<TDbContext>>
        where TDbContext : DbContext
    {
        public InMemoryDatabaseModule(Application application) : base(application)
        {
        }

        public override string GetConfigKey()
        {
            return $"Db:InMemory:{typeof(TDbContext).Name}";
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddDbContext<TDbContext>((p, options) =>
            {
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .UseInMemoryDatabase(GetConfig().Database);
                GetConfig().Configure?.Invoke((DbContextOptionsBuilder<TDbContext>)options, p, configuration,
                    environment);
            });
        }
    }
}
