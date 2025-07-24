using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data.TPH;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class TPHTests : BaseTest<TPHDbContextsTestScope>
{
    public TPHTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TPHGetAll()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TPHDbContext>();
        var records = await dbContext.Records.ToListAsync();
        records.Should().NotBeEmpty();
        records.Count.Should().Be(2);
    }

    [Fact]
    public async Task TPHGetFirsts()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TPHDbContext>();
        var records = await dbContext.Firsts.ToListAsync();
        records.Should().NotBeEmpty();
        records.Count.Should().Be(1);
        records.First().Should().BeOfType<FirstTPHClass>();
        records.First().Type.Should().Be(TPHType.First);
        records.First().Config.Should().BeOfType<FirstTPHClassConfig>();
        records.First().Config.First.Should().Be("456");
    }

    [Fact]
    public async Task TPHGetSeconds()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TPHDbContext>();
        var records = await dbContext.Seconds.ToListAsync();
        records.Should().NotBeEmpty();
        records.Count.Should().Be(1);
        records.First().Should().BeOfType<SecondTPHClass>();
        records.First().Type.Should().Be(TPHType.Second);
        records.First().Config.Should().BeOfType<SecondTPHClassConfig>();
        records.First().Config.Second.Should().Be("456");
    }
}
