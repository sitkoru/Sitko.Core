using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.ClickHouse.Linq2db;

public class ClickHouseLinq2dbModule<TDbContext> : BaseApplicationModule<ClickHouseLinq2dbModuleOptions>
    where TDbContext : BaseClickHouseDbContext
{
    public override string OptionsKey => $"ClickHouse:Db:{typeof(TDbContext).Name}";

    public override string[] OptionKeys => ["ClickHouse:Db:Default", OptionsKey];

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        ClickHouseLinq2dbModuleOptions options) => [typeof(ClickHouseModule)];

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ClickHouseLinq2dbModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddScoped<TDbContext>();
    }
}

public class ClickHouseLinq2dbModuleOptions : BaseModuleOptions
{
}
