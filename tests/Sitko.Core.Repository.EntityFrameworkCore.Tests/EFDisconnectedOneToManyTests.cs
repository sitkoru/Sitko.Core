using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFDisconnectedOneToManyTests : BaseTest<EFTestScope>
{
    public EFDisconnectedOneToManyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Add()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => !b.Foos.Any()));
            originalBar.Should().NotBeNull();
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id));
            bar.Should().NotBeNull();
        }

        bar!.Foos.Should().BeEmpty();
        bar.Foos.Add(new FooModel());

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
            updatedBar!.Foos.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task Append()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
            originalBar.Should().NotBeNull();
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id).Include(b => b.Foos));
            bar.Should().NotBeNull();
        }

        bar!.Foos.Should().NotBeEmpty();
        var count = bar.Foos.Count;

        bar.Foos.Add(new FooModel { Id = Guid.NewGuid() });

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
            updatedBar!.Foos.Should().NotBeEmpty();
            updatedBar.Foos.Should().HaveCount(count + 1);
        }
    }

    [Fact]
    public async Task AddNew()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => !b.Foos.Any()));
            Assert.NotNull(originalBar);
        }

        Assert.Empty(originalBar.Foos);

        var foo = new FooModel();

        originalBar.Foos.Add(foo);

        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository3.UpdateAsync(originalBar);
            Assert.True(updateResult.IsSuccess);
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
            Assert.NotEmpty(updatedBar!.Foos);
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
            originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
            Assert.NotNull(bar);
        }

        Assert.NotEmpty(bar.Foos);
        var foo = bar.Foos.OrderBy(_ => Guid.NewGuid()).First(); // change random
        var newText = Guid.NewGuid().ToString();
        foo.FooText = newText;

        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository3.UpdateAsync(bar, originalBar);
            Assert.True(updateResult.IsSuccess);
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Foos));
            Assert.Equal(newText, updatedBar!.Foos.First(f => f.Id == foo.Id).FooText);
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
            originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
            Assert.NotNull(bar);
        }

        Assert.NotEmpty(bar.Foos);
        var count = bar.Foos.Count;
        var foo1 = new FooModel();
        var foo2 = new FooModel();
        bar.Foos.Remove(bar.Foos.OrderBy(_ => Guid.NewGuid()).First());
        bar.Foos.Add(foo1);
        bar.Foos.Add(foo2);

        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult = await repository3.UpdateAsync(bar, originalBar);
            Assert.True(updateResult.IsSuccess);
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Foos));
            Assert.NotEmpty(updatedBar!.Foos);
            Assert.Equal(count + 1, updatedBar.Foos.Count);
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
            originalBar = await repository1.GetAsync(q => q.Where(b => b.Foos.Any()).Include(b => b.Foos));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Foos));
            Assert.NotNull(bar);
        }


        Assert.NotNull(bar);
        Assert.NotEmpty(bar.Foos);
        var count = bar.Foos.Count;
        Assert.Equal(3, count);

        bar.Foos.Remove(bar.Foos.OrderBy(_ => Guid.NewGuid()).First()); // delete random
        using (var scope3 = scope.CreateScope())
        {
            var repository3 = scope3.ServiceProvider.GetRequiredService<BarRepository>();
            var updateResult =
                await repository3.UpdateAsync(bar, originalBar);
            Assert.True(updateResult.IsSuccess);
            updateResult.Changes.Should().ContainSingle();
        }

        using (var finalScope = scope.CreateScope())
        {
            var repository = finalScope.ServiceProvider.GetRequiredService<BarRepository>();
            var updatedBar =
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Foos));
            Assert.Equal(count - 1, updatedBar!.Foos.Count);
        }
    }
}
