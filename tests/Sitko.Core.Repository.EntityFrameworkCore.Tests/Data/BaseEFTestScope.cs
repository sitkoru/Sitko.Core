using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;

public abstract class BaseEFTestScope : DbBaseTestScope<HostApplicationBuilder, TestDbContext, BaseEFTestConfig>
{
    protected override IHost BuildApplication(HostApplicationBuilder builder) => builder.Build();

    protected override HostApplicationBuilder CreateHostBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddSitkoCore();
        return builder;
    }
}

public class BaseEFTestConfig : BaseDbTestConfig
{
    public override bool UsePostgres => true;
}
