using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration.UserSecrets;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

[assembly: UserSecretsId("test")]

namespace Sitko.Core.Repository.Tests;

public abstract class BasicRepositoryTests<TScope> : BaseTest<TScope> where TScope : IBaseTestScope
{
    protected BasicRepositoryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Update()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();

        var item = await repository.GetAsync();

        Assert.NotNull(item);
        var oldValue = item.Status;
        item.Status = TestStatus.Disabled;

        var result = await repository.UpdateAsync(item);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Changes);
        Assert.NotEmpty(result.Changes.Where(c => c.Name == nameof(item.Status)));
        var change = result.Changes.First(c => c.Name == nameof(item.Status));
        Assert.Equal(oldValue, change.OriginalValue);
        Assert.Equal(item.Status, change.CurrentValue);
        await repository.RefreshAsync(item);
        Assert.Equal(TestStatus.Disabled, item.Status);
    }

    [Fact]
    public async Task Validation()
    {
        var scope = await GetScopeAsync();
        var validator = scope.GetServices<IValidator>();
        Assert.NotEmpty(validator);

        var typedValidator = scope.GetServices<IValidator<TestModel>>();
        Assert.NotEmpty(typedValidator);

        var repository = scope.GetService<IRepository<TestModel, Guid>>();

        var item = await repository.GetAsync();

        Assert.NotNull(item);
        item.Status = TestStatus.Error;

        var result = await repository.UpdateAsync(item);
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        var error = result.Errors.First();
        Assert.Equal(nameof(TestModel.Status), error.PropertyName);
        Assert.Equal("Status can't be error", error.ErrorMessage);
    }

    [Fact]
    public async Task Refresh()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();

        var item = await repository.GetAsync();

        Assert.NotNull(item);
        var oldValue = item.Status;
        item.Status = TestStatus.Disabled;

        Assert.NotEqual(oldValue, item.Status);
        var newItem = await repository.RefreshAsync(item);
        Assert.Equal(oldValue, newItem.Status);
    }


    [Fact]
    public async Task JsonConditions()
    {
        var json = "[{\"conditions\":[{\"property\":\"fooId\",\"operator\":1,\"value\":1}]}]";

        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();

        Assert.NotNull(repository);

        var items = await repository.GetAllAsync(q => q.WhereByString(json));

        Assert.NotEqual(0, items.itemsCount);
        Assert.Single(items.items);
    }

    [Fact]
    public async Task IncludeSingle()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<BarModel, Guid>>();

        Assert.NotNull(repository);

        var item = await repository.GetAsync(query => query.Where(e => e.TestId != null).Include(e => e.Test));
        Assert.NotNull(item);
        Assert.NotNull(item.Test);
        Assert.Equal(1, item.Test!.FooId);
    }

    [Fact]
    public async Task IncludeCollection()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();

        Assert.NotNull(repository);

        var item = await repository.GetAsync(query => query.Where(model => model.Bars.Any()).Include(e => e.Bars));
        Assert.NotNull(item);
        Assert.NotNull(item.Bars);
        Assert.NotEmpty(item.Bars);
        Assert.Single(item.Bars);
        Assert.Equal(item.Id, item.Bars.First().TestId);
    }


    [Fact]
    public async Task IncludeWithPagination()
    {
        var scope = await GetScopeAsync();

        var repository = scope.GetService<IRepository<TestModel, Guid>>();

        Assert.NotNull(repository);

        var item = await repository.GetAllAsync(query => query.Include(e => e.Bars).Take(1));
        Assert.Single(item.items);
    }

    [Fact]
    public async Task ThreadSafe()
    {
        var scope = await GetScopeAsync();

        var testRepository = scope.GetService<IRepository<TestModel, Guid>>();
        var barRepository = scope.GetService<IRepository<TestModel, Guid>>();

        var tasks = new List<Task> { testRepository.GetAllAsync(), barRepository.GetAllAsync() };

        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            Assert.True(task.IsCompletedSuccessfully);
        }
    }

    [Fact]
    public async Task Sum()
    {
        var scope = await GetScopeAsync();
        var repository = scope.GetService<IRepository<TestModel, Guid>>();
        var sum = await repository.SumAsync(t => t.FooId);
        var all = await repository.GetAllAsync();
        var allSum = all.items.Sum(t => t.FooId);
        Assert.Equal(allSum, sum);
    }

    [Fact]
    public async Task CrossRepository()
    {
        var scope = await GetScopeAsync();
        var barRepository = scope.GetService<IRepository<BarModel, Guid>>();
        var fooRepository = scope.GetService<IRepository<FooModel, Guid>>();

        var bars = (await barRepository.GetAllAsync()).items;
        bars.Should().NotBeEmpty();

        var foos = (await fooRepository.GetAllAsync(query => query.Include(f => f.Bar))).items;
        foos.Should().NotBeEmpty();

        var fooBars = foos.Where(f => f.Bar is not null).Select(f => f.Bar!).ToList();

        var bar = bars.First(b => fooBars.Any(fb => fb.Id == b.Id));

        var foo = new FooModel { Id = Guid.NewGuid(), FooText = "Foo", Bar = bar };
        var res = await fooRepository.AddAsync(foo);
        res.IsSuccess.Should().BeTrue();
    }
}
