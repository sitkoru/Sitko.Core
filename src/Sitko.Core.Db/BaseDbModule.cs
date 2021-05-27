using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db
{
    public interface IDbModule : IApplicationModule
    {
    }

    public abstract class BaseDbModule<TDbContext, TConfig> : BaseApplicationModule<TConfig>, IDbModule
        where TDbContext : DbContext
        where TConfig : BaseDbModuleConfig<TDbContext>, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services, TConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddHealthChecks().AddDbContextCheck<TDbContext>($"DB {typeof(TDbContext)} check");
        }
    }

    public abstract class BaseDbModuleConfig<TDbContext> : BaseModuleConfig where TDbContext : DbContext
    {
        public string Database { get; set; } = "dbname";

        public Action<DbContextOptionsBuilder<TDbContext>, IServiceProvider, IConfiguration, IHostEnvironment>?
            Configure { get; set; }
    }
}
