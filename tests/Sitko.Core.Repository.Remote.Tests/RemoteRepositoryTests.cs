using FluentAssertions;
using Sitko.Core.Repository.Remote.Tests.Data;
using Sitko.Core.Repository.Tests;
using Sitko.Core.Repository.Tests.Data;
using Xunit;

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

        result.items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Count()
    {
        var scope = await GetScopeAsync();
        var repo = scope.GetService<TestRemoteRepository>();
        var result = await repo.CountAsync();

        result.Should().Be(6);
    }

    [Fact]
    public async Task CountWithCondition()
    {
        var scope = await GetScopeAsync();
        var repo = scope.GetService<TestRemoteRepository>();
        var result = await repo.CountAsync(q => q.Where(t => t.FooId == 5));
        result.Should().Be(2);
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
        Assert.NotNull(item.Bars);
        Assert.NotEmpty(item.Bars);
        Assert.Single(item.Bars);
        var bar = item.Bars.First();
        Assert.Equal(item.Id, bar.TestId);
        Assert.NotEmpty(bar.Foos);
    }

    [Fact]
    public async Task ThenIncludeByName()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();
        Assert.NotNull(repository);

        var item = await repository.GetAsync(query => query.Where(model => model.Bars.Any())
            .Include($"{nameof(TestModel.Bars)}.{nameof(BarModel.Foos)}"));
        Assert.NotNull(item);
        Assert.NotNull(item.Bars);
        Assert.NotEmpty(item.Bars);
        Assert.Single(item.Bars);
        var bar = item.Bars.First();
        Assert.Equal(item.Id, bar.TestId);
        Assert.NotEmpty(bar.Foos);
    }

    [Fact]
    public async Task IncludeWithCondition()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();
        Assert.NotNull(repository);

        var item = await repository.GetAsync(query => query.Where(model => model.Bars.Any())
            .Include(testModel => testModel.Bars.Where(model => model.TestId != null)));
        Assert.NotNull(item);
        Assert.NotNull(item.Bars);
        Assert.NotEmpty(item.Bars);
        Assert.Single(item.Bars);
    }

    [Fact]
    public void MultipleIncludes()
    {
        var query = new RemoteRepositoryQuery<TestModel>();
        query.Include(model => model.Bars).Include(model => model.Bars);
        var serialized = query.Serialize();
        serialized.Data.Includes.Should().HaveCount(2);
        serialized.Data.Includes.Should().AllBeAssignableTo<IInclude>();
    }

    [Fact]
    public void MultipleIncludesWithThen()
    {
        var query = new RemoteRepositoryQuery<TestModel>();
        query.Include(model => model.Bars).Include(model => model.Bars).ThenInclude(model => model.Foos);
        var serialized = query.Serialize();
        serialized.Data.Includes.Should().HaveCount(2);
        serialized.Data.Includes[0].Should().BeOfType<Include<List<BarModel>>>();
        serialized.Data.Includes[1].Should().BeOfType<Include<List<FooModel>, BarModel>>();
        serialized.Data.Includes[1].As<Include<List<FooModel>, BarModel>>().Previous.Should()
            .BeOfType<Include<List<BarModel>>>();
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Contains(bool useList)
    {
        var scope = await GetScopeAsync();
        var repo = scope.GetService<TestRemoteRepository>();
        var model = await repo.GetAsync();
        model.Should().NotBeNull();
        var ids = new List<Guid> { model!.Id, Guid.NewGuid(), Guid.NewGuid() };
        var result = await GetOne<TestModel, Guid>(scope, ids, useList);
        result.Should().NotBeNull();
        result!.Id.Should().Be(model.Id);
    }

    private static async Task<T?> GetOne<T, TPk>(RemoteRepositoryTestScope scope, IEnumerable<TPk> ids, bool useList)
        where T : class, IEntity<TPk> where TPk : notnull
    {
        var repo = scope.GetService<IRepository<T, TPk>>();
        IEnumerable<TPk> data = useList ? ids.ToList() : ids.ToArray();
        return await repo.GetAsync(q => q.Where(model => data.Contains(model.Id)));
    }
}
