using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Infrastructure.Db;

namespace Sitko.Core.Db.InMemory
{
    public class InMemoryDatabaseModule<TDbContext> : BaseDbModule<TDbContext, InMemoryDatabaseModuleConfig<TDbContext>>
        where TDbContext : DbContext
    {
        protected override void CheckConfig()
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
                Config.Configure?.Invoke(options as DbContextOptionsBuilder<TDbContext>, p, configuration, environment);
            });
        }
    }

    public class InMemoryDatabaseModuleConfig<TDbContext> : BaseDbModuleConfig<TDbContext> where TDbContext : DbContext
    {
        public InMemoryDatabaseModuleConfig(string databaseName) : base(databaseName)
        {
        }
    }
}
