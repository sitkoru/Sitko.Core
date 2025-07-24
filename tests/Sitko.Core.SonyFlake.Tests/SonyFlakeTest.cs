using Microsoft.Extensions.Hosting;
using Sitko.Core.IdProvider;
using Sitko.Core.IdProvider.SonyFlake;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.SonyFlake.Tests;

public class SonyFlakeTest : BaseTest<SonyFlakeTestScope>
{
    public SonyFlakeTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Generate()
    {
        var scope = await GetScopeAsync();

        var provider = scope.GetService<IIdProvider>();

        var id = await provider.NextAsync();

        Assert.True(id > 0);
    }
}

public class SonyFlakeTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name).AddSonyFlakeIdProvider();
}
