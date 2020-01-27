using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Health;

namespace Sitko.Core.Db
{
    public abstract class BaseDbModule<TDbContext, TConfig> : BaseApplicationModule<TConfig>
        where TDbContext : DbContext
        where TConfig : BaseDbModuleConfig<TDbContext>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHealthChecks().AddDbContextCheck<TDbContext>($"DB {typeof(TDbContext)} check");
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(HealthModule)};
        }
    }

    public abstract class BaseDbModuleConfig<TDbContext> where TDbContext : DbContext
    {
        public BaseDbModuleConfig(string database)
        {
            Database = database;
        }

        public string Database { get; }

        public Action<DbContextOptionsBuilder<TDbContext>, IServiceProvider, IConfiguration, IHostEnvironment> Configure
        {
            get;
            set;
        }
    }
}
