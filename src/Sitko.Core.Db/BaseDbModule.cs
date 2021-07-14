using System;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Db
{
    using System.Text.Json.Serialization;

    public interface IDbModule : IApplicationModule
    {
    }

    public abstract class BaseDbModule<TDbContext, TConfig> : BaseApplicationModule<TConfig>, IDbModule
        where TDbContext : DbContext
        where TConfig : BaseDbModuleOptions<TDbContext>, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddHealthChecks().AddDbContextCheck<TDbContext>($"DB {typeof(TDbContext)} check");
        }
    }

    public abstract class BaseDbModuleOptions<TDbContext> : BaseModuleOptions where TDbContext : DbContext
    {
        public string Database { get; set; } = "dbname";

        [JsonIgnore]
        public Action<DbContextOptionsBuilder<TDbContext>, IServiceProvider, IConfiguration, IHostEnvironment>?
            ConfigureDbContextOptions { get; set; }
    }

    public abstract class BaseDbModuleOptionsValidator<TOptions, TDbContext> : AbstractValidator<TOptions>
        where TOptions : BaseDbModuleOptions<TDbContext> where TDbContext : DbContext
    {
    }
}
