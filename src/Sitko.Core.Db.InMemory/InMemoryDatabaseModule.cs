using System;
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
        public InMemoryDatabaseModule(InMemoryDatabaseModuleConfig<TDbContext> config, Application application) : base(
            config, application)
        {
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.Database))
            {
                throw new ArgumentException("Empty inmemory database name");
            }
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddDbContext<TDbContext>((p, options) =>
            {
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .UseInMemoryDatabase(Config.Database);
                Config.Configure?.Invoke((DbContextOptionsBuilder<TDbContext>) options, p, configuration, environment);
            });
        }
    }
}
