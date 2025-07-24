using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFDisconnectedOneToOneTests : BaseTest<EFTestScope>
{
    public EFDisconnectedOneToOneTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Set()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId == null));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
            Assert.NotNull(bar);
        }

        Assert.Null(bar.Test);
        Assert.Equal(default, bar.TestId);

        AddOrUpdateOperationResult<TestModel, Guid> newTestResult;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
            newTestResult = await repository2.AddAsync(await repository2.NewAsync());
        }

        Assert.True(newTestResult.IsSuccess);

        bar.Test = newTestResult.Entity;

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
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
            Assert.Equal(newTestResult.Entity.Id, updatedBar!.TestId);
        }
    }

    [Fact]
    public async Task SetViaProperty()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId == null));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar.Id));
            Assert.NotNull(bar);
        }

        Assert.Null(bar.Test);
        Assert.Equal(default, bar.TestId);

        AddOrUpdateOperationResult<TestModel, Guid> newTestResult;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
            newTestResult = await repository2.AddAsync(await repository2.NewAsync());
        }

        Assert.True(newTestResult.IsSuccess);

        bar.TestId = newTestResult.Entity.Id;


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
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
            Assert.Equal(newTestResult.Entity.Id, updatedBar!.TestId);
        }
    }

    [Fact]
    public async Task UpdateViaProperty()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null).Include(b => b.Test));
            originalBar.Should().NotBeNull();
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar!.Id).Include(b => b.Test));
            bar.Should().NotBeNull();
        }

        bar!.Test.Should().NotBeNull();
        bar.TestId.Should().NotBe(default(Guid));

        AddOrUpdateOperationResult<TestModel, Guid> newTestResult;
        using (var scope2 = scope.CreateScope())
        {
            var repository2 = scope2.ServiceProvider.GetRequiredService<IRepository<TestModel, Guid>>();
            newTestResult = await repository2.AddAsync(await repository2.NewAsync());
        }

        newTestResult.IsSuccess.Should().BeTrue();

        bar.TestId = newTestResult.Entity.Id;


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
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id));
            updatedBar!.TestId.Should().Be(newTestResult.Entity.Id);
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
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null).Include(b => b.Test));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Test));
            Assert.NotNull(bar);
        }


        Assert.NotNull(bar.Test);
        bar.Test!.FooId = 10;

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
                await repository.GetAsync(q => q.Where(b => b.Id == bar.Id).Include(b => b.Test));
            Assert.Equal(10, updatedBar!.Test!.FooId);
        }
    }

    [Fact]
    public async Task Unset()
    {
        var scope = await GetScopeAsync();
        BarModel? originalBar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            originalBar = await repository1.GetAsync(q => q.Where(b => b.TestId != null).Include(b => b.Test));
            Assert.NotNull(originalBar);
        }

        BarModel? bar;
        using (var scope1 = scope.CreateScope())
        {
            var repository1 = scope1.ServiceProvider.GetRequiredService<BarRepository>();
            bar = await repository1.GetAsync(q => q.Where(b => b.Id == originalBar.Id).Include(b => b.Test));
            Assert.NotNull(bar);
        }

        Assert.NotNull(bar.Test);
        bar.Test = null;

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
            Assert.Null(updatedBar!.Test);
            Assert.Null(updatedBar.TestId);
        }
    }
}
