using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Sitko.Core.Repository.Remote.Tests.Data;
using Sitko.Core.Repository.Tests;
using Sitko.Core.Repository.Tests.Data;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.Remote.Tests;

public class RemoteRepositoryTests : BasicRepositoryTests<RemoteRepositoryTestScope>
{
    public RemoteRepositoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GetAll()
    {
        var scope = await GetScopeAsync();
        var repo = scope.GetService<TestRemoteRepository>();
        var result = await repo.GetAllAsync();

        Assert.NotNull(result.items);
    }

    [Fact]
    public async Task GetNullable()
    {
        var scope = await GetScopeAsync();
        var repo = scope.GetService<TestRemoteRepository>();
        var result = await repo.GetAsync(q => q.Where(t => t.FooId == 6));
        Assert.Null(result);
    }

    [Fact]
    public async Task Get()
    {
        var scope = await GetScopeAsync();
        var repo = scope.GetService<TestRemoteRepository>();
        var result = await repo.GetAsync(q => q.Where(t => t.FooId == 5));
        Assert.NotNull(result);
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
        Assert.NotNull(item!.Bars);
        Assert.NotEmpty(item.Bars);
        Assert.Single(item.Bars);
        var bar = item.Bars.First();
        Assert.Equal(item.Id, bar.TestId);
        Assert.NotEmpty(bar.Foos);
    }

    [Fact]
    public void MultipleIncludes()
    {
        var query = new RemoteRepositoryQuery<TestModel>();
        query.Include(model => model.Bars).Include(model => model.Bars);
        var serialized = query.Serialize();
        serialized.Data.Includes.Should().HaveCount(2);
        serialized.Data.Includes.Should().AllBe(nameof(TestModel.Bars));
    }

    [Fact]
    public void MultipleIncludesWithThen()
    {
        var query = new RemoteRepositoryQuery<TestModel>();
        query.Include(model => model.Bars).Include(model => model.Bars).ThenInclude(model => model.Foos);
        var serialized = query.Serialize();
        serialized.Data.Includes.Should().HaveCount(2);
        serialized.Data.Includes[0].Should().Be(nameof(TestModel.Bars));
        serialized.Data.Includes[1].Should().Be(nameof(TestModel.Bars) + "." + nameof(BarModel.Foos));
    }

    [Fact]
    public void WhereByString()
    {
        var query = new RemoteRepositoryQuery<TestModel>();
        query.Include(model => model.Bars).Where("bla", new object[] { 1 });
        var serialized = query.Serialize();
        serialized.Data.WhereByString.Should().HaveCount(1);
        serialized.Data.WhereByString.Should().Contain(tuple => tuple.WhereStr == "bla" && tuple.Values!.Contains(1));
    }
}
