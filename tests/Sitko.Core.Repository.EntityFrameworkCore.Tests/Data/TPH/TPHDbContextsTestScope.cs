using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data.TPH;

public class TPHDbContextsTestScope : DbBaseTestScope<HostApplicationBuilder,TPHDbContext, TPHDbContextsTestScopeConfig>
{
    protected override async Task InitDbContextAsync(TPHDbContext dbContext)
    {
        await base.InitDbContextAsync(dbContext);
        await dbContext.AddAsync(
            new FirstTPHClass { Bar = "213", Config = new FirstTPHClassConfig { First = "456" }, Foo = "789" });
        await dbContext.AddAsync(
            new SecondTPHClass { Baz = "213", Config = new SecondTPHClassConfig { Second = "456" }, Foo = "789" });
        await dbContext.SaveChangesAsync();
    }

    protected override HostApplicationBuilder CreateHostBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddSitkoCore();
        return builder;
    }

    protected override IHost BuildApplication(HostApplicationBuilder builder) => builder.Build();
}

public class TPHDbContextsTestScopeConfig : BaseDbTestConfig
{
    public override bool UsePostgres { get; set; } = true;
}
