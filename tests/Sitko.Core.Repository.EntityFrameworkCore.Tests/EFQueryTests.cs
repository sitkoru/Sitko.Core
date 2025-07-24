using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.Repository.EntityFrameworkCore.Tests.Data;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests;
#pragma warning disable CA1304
#pragma warning disable CA1311
public class EFQueryTests : BaseTest<EFTestScope>
{
    public EFQueryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task IsEquals()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.Equal, 1));
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId == 1);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotEquals()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.NotEqual, 1));
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId != 1);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task Greater()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.Greater, 1));
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId > 1);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task GreaterOrEqual()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.GreaterOrEqual, 1));
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId >= 1);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task Less()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.Less, 1));
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId < 1);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task LessOrEqual()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.LessOrEqual, 1));
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId <= 1);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task In()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.In, new[] { 1, 2, 3 }));
        var dbQuery = dbContext.Set<TestModel>().Where(model => new[] { 1, 2, 3 }.Contains(model.FooId));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotIn()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.NotIn,
            new[] { 1, 2, 3 }));
        var dbQuery = dbContext.Set<TestModel>().Where(model => !new[] { 1, 2, 3 }.Contains(model.FooId));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task IsNull()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.IsNull));
        var dbQuery = dbContext.Set<BarModel>().Where(model => model.Baz == null);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotNull()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotNull));
        var dbQuery = dbContext.Set<BarModel>().Where(model => model.Baz != null);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task Contains()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.Contains, "123"));
        var dbQuery = dbContext.Set<BarModel>().Where(model => model.Baz!.Contains("123"));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotContains()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotContains, "123"));
        var dbQuery = dbContext.Set<BarModel>().Where(model => !model.Baz!.Contains("123"));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task ContainsCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.ContainsCaseInsensitive,
            "AbC"));
        var dbQuery = dbContext.Set<BarModel>()
            .Where(model => model.Baz!.ToLower().Contains("AbC".ToLower()));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotContainsCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotContainsCaseInsensitive,
            "AbC"));
        var dbQuery = dbContext.Set<BarModel>()
            .Where(model => !model.Baz!.ToLower().Contains("AbC".ToLower()));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task StartsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.StartsWith, "123"));
        var dbQuery = dbContext.Set<BarModel>()
            .Where(model => model.Baz!.StartsWith("123"));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotStartsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotStartsWith, "123"));
        var dbQuery = dbContext.Set<BarModel>()
            .Where(model => !model.Baz!.StartsWith("123"));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task StartsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.StartsWithCaseInsensitive,
            "AbC"));
        var dbQuery = dbContext.Set<BarModel>().Where(model =>
            model.Baz!.ToLower().StartsWith("AbC".ToLower()));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotStartsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz),
            QueryContextOperator.NotStartsWithCaseInsensitive, "AbC"));
        var dbQuery = dbContext.Set<BarModel>().Where(model =>
            !model.Baz!.ToLower().StartsWith("AbC".ToLower()));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task EndsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();

        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.EndsWith, "123"));
        var dbQuery = dbContext.Set<BarModel>()
            .Where(model => model.Baz!.EndsWith("123"));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotEndsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotEndsWith, "AbC"));
        var dbQuery = dbContext.Set<BarModel>()
            .Where(model => !model.Baz!.EndsWith("AbC"));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task EndsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.EndsWithCaseInsensitive,
            "AbC"));
        var dbQuery = dbContext.Set<BarModel>().Where(model =>
            model.Baz!.ToLower().EndsWith("AbC".ToLower()));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task NotEndsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotEndsWithCaseInsensitive,
            "AbC"));
        var dbQuery = dbContext.Set<BarModel>().Where(model =>
            !model.Baz!.ToLower().EndsWith("AbC".ToLower()));
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task MultipleConditions()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextConditionsGroup(
            new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.Equal, 1),
            new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.NotEqual, 2)
        ));
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId == 1 || model.FooId != 2);
        CompareSql(query, dbQuery);
    }

    [Fact]
    public async Task MultipleConditionGroups()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(
            new QueryContextConditionsGroup(new QueryContextCondition(nameof(TestModel.FooId),
                QueryContextOperator.Equal, 1)),
            new QueryContextConditionsGroup(new QueryContextCondition(nameof(TestModel.FooId),
                QueryContextOperator.NotEqual, 2))
        );
        var dbQuery = dbContext.Set<TestModel>().Where(model => model.FooId == 1 && model.FooId != 2);
        CompareSql(query, dbQuery);
    }

    private static void CompareSql<TItem>(EFRepositoryQuery<TItem> query, IQueryable<TItem> expectedQuery)
        where TItem : class
    {
        var sql = query.QueryString.Replace("\n", " ").Replace("\r", "");
        var expectedSql = expectedQuery.ToQueryString().Replace("\n", " ").Replace("\r", "");
        sql.Should().Be(expectedSql);
    }
}
#pragma warning restore CA1311
#pragma warning restore CA1304
