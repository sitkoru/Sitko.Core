using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFDisconnectedTests : BaseTest<EFTestScope>
{
    public EFDisconnectedTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task DisconnectedProperty()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository2.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
            Assert.NotNull(bar);
        }

        Assert.Null(bar.Baz);
        bar.Baz = "123";

        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository2.UpdateAsync(bar, originalBar);
            Assert.True(updateResult.IsSuccess);
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
            Assert.Equal("123", updatedBar!.Baz);
        }
    }

    [Fact]
    public async Task DisconnectedPropertyNoOriginal()
    {
        var scope = await GetScopeAsync();
        BarModel? bar;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository2.GetAsync(q => q.Where(b => b.TestId != null));
            bar.Should().NotBeNull();
        }

        bar!.Baz.Should().BeNull();
        bar.Baz = "123";

        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository2.UpdateAsync(bar);
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
            updatedBar!.Baz.Should().BeEquivalentTo("123");
        }
    }

    [Fact]
    public async Task DisconnectedDelete()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null));
            Assert.NotNull(originalBar);
        }

        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository2.DeleteAsync(originalBar);
            Assert.True(updateResult);
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
            Assert.Null(updatedBar);
        }
    }
}
