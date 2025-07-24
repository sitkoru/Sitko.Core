using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFDisconnectedManyToManyTests : BaseTest<EFTestScope>
{
    public EFDisconnectedManyToManyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task SkipNavigations()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var dbContext = scope1.ServiceProvider.GetRequiredService<TestDbContext>();
            originalBar = await dbContext.Set<BarModel>().Where(b => b.TestId != null).Include(b => b.Foos)
                .Include(b => b.BazModels)
                .FirstOrDefaultAsync();
            Assert.NotNull(originalBar);
        }

        Assert.Single(originalBar.Foos);
        var foo = originalBar.Foos.First();
        Assert.Empty(foo.BazModels);

        using (var scope2 = scope.CreateScope())
        {
            var dbContext = scope2.ServiceProvider.GetRequiredService<TestDbContext>();
            var bazModels = await dbContext.Set<BazModel>().Where(b => b.Foos.Contains(foo)).ToListAsync();
            Assert.NotEmpty(bazModels);
            Assert.Empty(foo.BazModels);
        }
    }

    [Fact]
    public async Task Add()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => !b.BazModels.Any()));
            originalBar.Should().NotBeNull();
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id));
            bar.Should().NotBeNull();
        }

        bar!.BazModels.Should().BeEmpty();

        AddOrUpdateOperationResult<BazModel, Guid> newTestResult;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<BazRepository>();
            newTestResult = await repository2.AddAsync(await repository2.NewAsync());
        }

        newTestResult.IsSuccess.Should().BeTrue();

        bar.BazModels.Add(newTestResult.Entity);

        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository3.UpdateAsync(bar, originalBar);
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.BazModels));
            updatedBar!.BazModels.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task UpdateRelationProperty()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.BazModels.Any()).Include(b => b.Foos));
            originalBar.Should().NotBeNull();
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id).Include(b => b.BazModels));
            bar.Should().NotBeNull();
        }

        bar!.BazModels.Should().NotBeEmpty();
        var foo = bar.BazModels.OrderBy(_ => Guid.NewGuid()).First(); // change random
        var newText = Guid.NewGuid().ToString();
        foo.Baz = newText;

        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository3.UpdateAsync(bar, originalBar);
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Foos));
            updatedBar!.BazModels.First(f => f.Id == foo.Id).Baz.Should().BeEquivalentTo(newText);
        }
    }

    [Fact]
    public async Task AddAndRemove()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.BazModels.Any()));
            originalBar.Should().NotBeNull();
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id));
            bar.Should().NotBeNull();
        }

        bar!.BazModels.Should().NotBeEmpty();
        var count = bar.BazModels.Count;
        var baz1 = new BazModel();
        var baz2 = new BazModel();
        bar.BazModels.Remove(bar.BazModels.OrderBy(_ => Guid.NewGuid()).First());
        bar.BazModels.Add(baz1);
        bar.BazModels.Add(baz2);

        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository3.UpdateAsync(bar, originalBar);
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.BazModels));
            updatedBar!.BazModels.Should().NotBeEmpty();
            updatedBar.BazModels.Count.Should().Be(count + 1);
        }
    }

    [Fact]
    public async Task Remove()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar =
                await repository1.GetAsync(q => q.Where(b => b.BazModels.Count > 1).Include(b => b.BazModels));
            originalBar.Should().NotBeNull();
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id).Include(b => b.BazModels));
            bar.Should().NotBeNull();
        }


        bar!.BazModels.Should().NotBeEmpty();
        var count = bar.BazModels.Count;
        count.Should().Be(2);

        bar.BazModels.Remove(bar.BazModels.OrderBy(_ => Guid.NewGuid()).First()); // delete random
        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult =
                await repository3.UpdateAsync(bar, originalBar);
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.BazModels));
            updatedBar!.BazModels.Count.Should().Be(count - 1);
        }
    }
}
