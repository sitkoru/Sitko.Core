using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests;
using Sitko.Core.Repository.Tests.Data;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;

public class EFTests : BasicRepositoryTests<EFTestScope>
{
    public EFTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task MultipleDbContexts()
    {
        var scope = await GetScopeAsync<MultipleDbContextsTestScope>();
        var barRepository = scope.GetService<IRepository<BarModel, Guid>>();
        var fooBarRepository = scope.GetService<IRepository<FooBarModel, Guid>>();

        var bars = (await barRepository.GetAllAsync()).items;
        bars.Should().NotBeEmpty();

        var fooBar = new FooBarModel { Id = Guid.NewGuid(), BarId = bars.First().Id };
        var res = await fooBarRepository.AddAsync(fooBar);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ThenInclude()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();
        Assert.NotNull(repository);

        var item = await repository.GetAsync(query => query.Where(model => model.Bars.Any())
            .Include(testModel => testModel.Bars).ThenInclude(barModel => barModel.Foos));
        Assert.NotNull(item);
        Assert.NotNull(item.Bars);
        Assert.NotEmpty(item.Bars);
        Assert.Single(item.Bars);
        var bar = item.Bars.First();
        Assert.Equal(item.Id, bar.TestId);
        Assert.NotEmpty(bar.Foos);
        var foo = bar.Foos.First();
        Assert.NotNull(foo.Bar);
        Assert.NotNull(foo.Bar!.Test);
    }

    [Fact]
    public async Task DeleteAllRaw()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<FooModel, Guid>>();
        Assert.NotNull(repository);
        var item = await repository.GetAsync();
        Assert.NotNull(item);

        var efRepository = repository as IEFRepository;
        efRepository.Should().NotBeNull();
        var deleted = await efRepository!.DeleteAllRawAsync($"\"{nameof(FooModel.Id)}\" = '{item.Id}'");
        deleted.Should().Be(1);
        item = await repository.GetByIdAsync(item.Id);
        item.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAll()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<FooModel, Guid>>();
        Assert.NotNull(repository);
        var items = await repository.CountAsync();
        items.Should().BeGreaterThan(0);

        var efRepository = repository as IEFRepository;
        efRepository.Should().NotBeNull();
        var deleted = await efRepository!.DeleteAllAsync();
        deleted.Should().Be(items);
        items = await repository.CountAsync();
        items.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAllCondition()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<FooModel, Guid>>();
        Assert.NotNull(repository);
        var item = await repository.GetAsync();
        Assert.NotNull(item);

        var efRepository = repository as IEFRepository<FooModel>;
        efRepository.Should().NotBeNull();
        var deleted = await efRepository!.DeleteAllAsync(model => model.Id == item.Id);
        deleted.Should().Be(1);
        item = await repository.GetByIdAsync(item.Id);
        item.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAllCondition()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<FooModel, Guid>>();
        Assert.NotNull(repository);
        var item = await repository.GetAsync();
        Assert.NotNull(item);
        var oldValue = item.FooText;
        var newText = Guid.NewGuid().ToString();
        oldValue.Should().NotBe(newText);

        var efRepository = repository as IEFRepository<FooModel>;
        efRepository.Should().NotBeNull();
        var updated = await efRepository!.UpdateAllAsync(model => model.Id == item.Id,
            calls => calls.SetProperty(model => model.FooText, newText));
        updated.Should().Be(1);
        item = await repository.RefreshAsync(item);
        item.FooText.Should().Be(newText);
    }

    [Fact]
    public async Task UpdateAll()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<FooModel, Guid>>();
        var newText = Guid.NewGuid().ToString();
        Assert.NotNull(repository);
        var items = await repository.GetAllAsync();
        items.items.Should().NotBeEmpty();
        items.items.Should().AllSatisfy(model => model.FooText.Should().NotBe(newText));

        var efRepository = repository as IEFRepository<FooModel>;
        efRepository.Should().NotBeNull();
        var updated = await efRepository!.UpdateAllAsync(calls => calls.SetProperty(model => model.FooText, newText));
        updated.Should().Be(items.items.Length);
        items = await scope.CreateScope().ServiceProvider.GetRequiredService<IRepository<FooModel, Guid>>()
            .GetAllAsync();
        items.items.Should().AllSatisfy(model => model.FooText.Should().Be(newText));
    }
}
