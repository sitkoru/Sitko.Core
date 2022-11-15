using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Db;

public interface IDbModule : IApplicationModule
{
}

public abstract class BaseDbModule<TDbContext, TConfig> : BaseApplicationModule<TConfig>, IDbModule
    where TDbContext : DbContext
    where TConfig : BaseDbModuleOptions<TDbContext>, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddHealthChecks().AddDbContextCheck<TDbContext>($"DB {typeof(TDbContext)} check");
    }
}

public abstract class BaseDbModuleOptions<TDbContext> : BaseModuleOptions where TDbContext : DbContext
{
    public string Database { get; set; } = "dbname";
    public bool EnableContextPooling { get; set; } = true;
    public bool IncludeErrorDetails { get; set; }
    public bool EnableSensitiveLogging { get; set; }

    [JsonIgnore]
    public Action<DbContextOptionsBuilder<TDbContext>, IServiceProvider, IApplicationContext>?
        ConfigureDbContextOptions { get; set; }
}

public abstract class BaseDbModuleOptionsValidator<TOptions, TDbContext> : AbstractValidator<TOptions>
    where TOptions : BaseDbModuleOptions<TDbContext> where TDbContext : DbContext
{
}

