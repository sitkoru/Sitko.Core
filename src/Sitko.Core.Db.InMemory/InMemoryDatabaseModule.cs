using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db.InMemory
{
    public class InMemoryDatabaseModule<TDbContext> : BaseApplicationModule<InMemoryDatabaseModuleConfig>
        where TDbContext : DbContext
    {
        protected override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.InMemoryDatabaseName))
            {
                throw new ArgumentException("Empty inmemory database name");
            }
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddDbContext<TDbContext>((p, options) =>
            {
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .UseInMemoryDatabase(Config.InMemoryDatabaseName).UseInternalServiceProvider(p);
                    Config.Configure?.Invoke(options, configuration, environment);
            });
        }
    }

    public class InMemoryDatabaseModuleConfig
    {
        public InMemoryDatabaseModuleConfig(string inMemoryDatabaseName)
        {
            InMemoryDatabaseName = inMemoryDatabaseName;
        }

        public string InMemoryDatabaseName { get; }
        public  Action<DbContextOptionsBuilder, IConfiguration, IHostEnvironment> Configure { get; set; }
    }
}
