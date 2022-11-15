using FluentAssertions;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests;
using Sitko.Core.Repository.Tests.Data;
using Xunit;
using Xunit.Abstractions;

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
}

