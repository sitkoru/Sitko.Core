using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFRepositoryGraphTests(ITestOutputHelper testOutputHelper) : BaseTest<EFTestScope>(testOutputHelper)
{
    [Fact]
    public async Task DetachedUpdateRemovesReference()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scope = await GetScopeAsync();
        var barRepository = scope.GetService<IRepository<BarModel, Guid>>();

        var bar = await barRepository.GetAsync(query =>
            query.Where(model => model.TestId != null).Include(model => model.Test), cancellationToken);
        bar.Should().NotBeNull();

        var oldBar = barRepository.CreateSnapshot(bar!);
        var updatedBar = barRepository.CreateSnapshot(bar!);
        updatedBar.TestId = null;
        updatedBar.Test = null;

        using (var updateScope = scope.CreateScope())
        {
            var detachedRepository =
                updateScope.ServiceProvider.GetRequiredService<IRepository<BarModel, Guid>>();
            var result = await detachedRepository.UpdateAsync(updatedBar, oldBar, cancellationToken);

            result.IsSuccess.Should().BeTrue();
            result.Changes.Should().Contain(change =>
                change.Name == nameof(BarModel.Test) && change.ChangeType == ChangeType.Deleted);
            result.Changes.Should().Contain(change =>
                change.Name == nameof(BarModel.TestId) && change.CurrentValue == null);
        }

        using var verificationScope = scope.CreateScope();
        var verificationRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRepository<BarModel, Guid>>();
        var persisted = await verificationRepository.GetByIdAsync(updatedBar.Id,
            query => query.Include(model => model.Test), cancellationToken);
        persisted.Should().NotBeNull();
        persisted!.TestId.Should().BeNull();
        persisted.Test.Should().BeNull();
    }

    [Fact]
    public async Task DetachedUpdateReplacesManyToManyCollection()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scope = await GetScopeAsync();
        var bazRepository = scope.GetService<IRepository<BazModel, Guid>>();
        var barRepository = scope.GetService<IRepository<BarModel, Guid>>();

        var (allBars, _) = await barRepository.GetAllAsync(cancellationToken);
        allBars.Length.Should().BeGreaterThan(0);

        var (bazItems, _) = await bazRepository.GetAllAsync(query => query.Include(model => model.Bars),
            cancellationToken);
        bazItems.Length.Should().BeGreaterThan(0);

        var baz = bazItems.First(model => model.Bars.Count > 0 && model.Bars.Count < allBars.Length);

        var replacementBar = allBars.FirstOrDefault(model =>
            baz.Bars.All(existing => existing.Id != model.Id));
        replacementBar.Should().NotBeNull();
        var replacementBarId = replacementBar!.Id;
        var removedBarId = baz.Bars.First().Id;

        var oldBaz = bazRepository.CreateSnapshot(baz);
        var updatedBaz = bazRepository.CreateSnapshot(baz);
        updatedBaz.Bars.RemoveAt(0);
        updatedBaz.Bars.Add(new BarModel { Id = replacementBarId });

        using (var updateScope = scope.CreateScope())
        {
            var detachedRepository =
                updateScope.ServiceProvider.GetRequiredService<IRepository<BazModel, Guid>>();
            var result = await detachedRepository.UpdateAsync(updatedBaz, oldBaz, cancellationToken);

            result.IsSuccess.Should().BeTrue();
            result.Changes.Should().Contain(change =>
                change.Name == nameof(BazModel.Bars) && change.ChangeType == ChangeType.Modified);
        }

        using var verificationScope = scope.CreateScope();
        var verificationRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRepository<BazModel, Guid>>();
        var persisted = await verificationRepository.GetByIdAsync(updatedBaz.Id,
            query => query.Include(model => model.Bars), cancellationToken);
        persisted.Should().NotBeNull();
        persisted!.Bars.Select(model => model.Id).Should().Contain(replacementBarId);
        persisted.Bars.Select(model => model.Id).Should().NotContain(removedBarId);
    }

    [Fact]
    public async Task DetachedUpdateAddsNewChildEntity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scope = await GetScopeAsync();
        var barRepository = scope.GetService<IRepository<BarModel, Guid>>();

        var bar = await barRepository.GetAsync(query =>
            query.Where(model => model.Foos.Count != 0).Include(model => model.Foos), cancellationToken);
        bar.Should().NotBeNull();

        var oldBar = barRepository.CreateSnapshot(bar!);
        var updatedBar = barRepository.CreateSnapshot(bar!);
        var newFoo = new FooModel
        {
            Id = Guid.NewGuid(),
            FooText = "detached child",
            BarId = updatedBar.Id,
            Bar = new BarModel { Id = updatedBar.Id }
        };
        updatedBar.Foos.Add(newFoo);

        using (var updateScope = scope.CreateScope())
        {
            var detachedRepository =
                updateScope.ServiceProvider.GetRequiredService<IRepository<BarModel, Guid>>();
            var result = await detachedRepository.UpdateAsync(updatedBar, oldBar, cancellationToken);

            result.IsSuccess.Should().BeTrue();
            result.Changes.Should().Contain(change =>
                change.Name == nameof(BarModel.Foos) && change.ChangeType == ChangeType.Modified);
        }

        using var verificationScope = scope.CreateScope();
        var verificationBarRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRepository<BarModel, Guid>>();
        var persistedBar = await verificationBarRepository.GetByIdAsync(updatedBar.Id,
            query => query.Include(model => model.Foos), cancellationToken);
        persistedBar.Should().NotBeNull();
        persistedBar!.Foos.Should().Contain(model => model.Id == newFoo.Id);

        var verificationFooRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRepository<FooModel, Guid>>();
        var persistedFoo = await verificationFooRepository.GetByIdAsync(newFoo.Id,
            query => query.Include(model => model.Bar), cancellationToken);
        persistedFoo.Should().NotBeNull();
        persistedFoo!.BarId.Should().Be(updatedBar.Id);
        persistedFoo.Bar.Should().NotBeNull();
    }

    [Fact]
    public async Task AddExternalAsyncAttachesExistingGraph()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scope = await GetScopeAsync();
        var barRepository = scope.GetService<IRepository<BarModel, Guid>>();
        var fooRepository = scope.GetService<IRepository<FooModel, Guid>>();

        var barCountBefore = await barRepository.CountAsync(cancellationToken);
        var bar = await barRepository.GetAsync(query =>
            query.Where(model => model.TestId != null), cancellationToken);
        bar.Should().NotBeNull();

        var externalFoo = new FooModel
        {
            Id = Guid.NewGuid(), FooText = "external", BarId = bar!.Id, Bar = new BarModel { Id = bar.Id }
        };

        using (var addScope = scope.CreateScope())
        {
            var detachedFooRepository =
                addScope.ServiceProvider.GetRequiredService<IRepository<FooModel, Guid>>();
            var result = await detachedFooRepository.AddExternalAsync(externalFoo, cancellationToken);

            result.IsSuccess.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        using var verificationScope = scope.CreateScope();
        var verificationFooRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRepository<FooModel, Guid>>();
        var verificationBarRepository =
            verificationScope.ServiceProvider.GetRequiredService<IRepository<BarModel, Guid>>();

        var barCountAfter = await verificationBarRepository.CountAsync(cancellationToken);
        barCountAfter.Should().Be(barCountBefore);

        var persistedFoo = await verificationFooRepository.GetByIdAsync(externalFoo.Id,
            query => query.Include(model => model.Bar), cancellationToken);
        persistedFoo.Should().NotBeNull();
        persistedFoo!.Bar.Should().NotBeNull();
        persistedFoo.Bar!.Id.Should().Be(bar.Id);

        var persistedBar = await verificationBarRepository.GetByIdAsync(bar.Id,
            query => query.Include(model => model.Foos), cancellationToken);
        persistedBar.Should().NotBeNull();
        persistedBar!.Foos.Should().Contain(model => model.Id == externalFoo.Id);
    }
}
