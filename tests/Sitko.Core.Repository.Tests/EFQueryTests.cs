using System.Threading.Tasks;
using FluentAssertions;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Repository.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.Tests;

public class EFQueryTests : BaseTest<EFTestScope>
{
    public EFQueryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Equals()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.Equal, 1));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" = 1");
    }

    [Fact]
    public async Task NotEquals()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.NotEqual, 1));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" <> 1");
    }

    [Fact]
    public async Task Greater()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.Greater, 1));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" > 1");
    }

    [Fact]
    public async Task GreaterOrEqual()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.GreaterOrEqual, 1));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" >= 1");
    }

    [Fact]
    public async Task Less()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.Less, 1));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" < 1");
    }

    [Fact]
    public async Task LessOrEqual()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.LessOrEqual, 1));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" <= 1");
    }

    [Fact]
    public async Task In()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.In, new[] { 1, 2, 3 }));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" IN (1, 2, 3)");
    }

    [Fact]
    public async Task NotIn()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<TestModel>(dbContext.Set<TestModel>());
        query.Where(new QueryContextCondition(nameof(TestModel.FooId), QueryContextOperator.NotIn,
            new[] { 1, 2, 3 }));
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE t.\"FooId\" NOT IN (1, 2, 3)");
    }

    [Fact]
    public async Task IsNull()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.IsNull));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NULL");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NULL)");
#endif
    }

    [Fact]
    public async Task NotNull()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotNull));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL)");
#endif
    }

    [Fact]
    public async Task Contains()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.Contains, "123"));
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE strpos(b.\"Baz\", '123') > 0");
    }

    [Fact]
    public async Task NotContains()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotContains, "123"));
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE NOT (strpos(b.\"Baz\", '123') > 0)");
    }

    [Fact]
    public async Task ContainsCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.ContainsCaseInsensitive,
            "AbC"));
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE strpos(lower(b.\"Baz\"), 'abc') > 0");
    }

    [Fact]
    public async Task NotContainsCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotContainsCaseInsensitive,
            "AbC"));
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE NOT (strpos(lower(b.\"Baz\"), 'abc') > 0)");
    }

    [Fact]
    public async Task StartsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.StartsWith, "123"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND (b.\"Baz\" LIKE '123%')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND (b.\"Baz\" LIKE '123%')");
#endif
    }

    [Fact]
    public async Task NotStartsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotStartsWith, "123"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND NOT (b.\"Baz\" LIKE '123%')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND NOT (b.\"Baz\" LIKE '123%')");
#endif
    }

    [Fact]
    public async Task StartsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.StartsWithCaseInsensitive,
            "AbC"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND (lower(b.\"Baz\") LIKE 'abc%')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND (lower(b.\"Baz\") LIKE 'abc%')");
#endif
    }

    [Fact]
    public async Task NotStartsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz),
            QueryContextOperator.NotStartsWithCaseInsensitive, "AbC"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND NOT (lower(b.\"Baz\") LIKE 'abc%')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND NOT (lower(b.\"Baz\") LIKE 'abc%')");
#endif
    }

    [Fact]
    public async Task EndsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.EndsWith, "123"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND (b.\"Baz\" LIKE '%123')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND (b.\"Baz\" LIKE '%123')");
#endif
    }

    [Fact]
    public async Task NotEndsWith()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotEndsWith, "AbC"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND NOT (b.\"Baz\" LIKE '%AbC')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND NOT (b.\"Baz\" LIKE '%AbC')");
#endif
    }

    [Fact]
    public async Task EndsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.EndsWithCaseInsensitive,
            "AbC"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND (lower(b.\"Baz\") LIKE '%abc')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND (lower(b.\"Baz\") LIKE '%abc')");
#endif
    }

    [Fact]
    public async Task NotEndsWithCaseInsensitive()
    {
        var scope = await GetScopeAsync();
        var dbContext = scope.GetService<TestDbContext>();
        var query = new EFRepositoryQuery<BarModel>(dbContext.Set<BarModel>());
        query.Where(new QueryContextCondition(nameof(BarModel.Baz), QueryContextOperator.NotEndsWithCaseInsensitive,
            "AbC"));
#if NET6_0_OR_GREATER
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE b.\"Baz\" IS NOT NULL AND NOT (lower(b.\"Baz\") LIKE '%abc')");
#else
        CompareSql(query,
            "SELECT b.\"Id\", b.\"Baz\", b.\"JsonModels\", b.\"TestId\" FROM \"BarModel\" AS b WHERE (b.\"Baz\" IS NOT NULL) AND NOT (lower(b.\"Baz\") LIKE '%abc')");
#endif
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
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE (t.\"FooId\" = 1) OR (t.\"FooId\" <> 2)");
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
        CompareSql(query,
            "SELECT t.\"Id\", t.\"FooId\", t.\"Status\" FROM \"TestModels\" AS t WHERE (t.\"FooId\" = 1) AND (t.\"FooId\" <> 2)");
    }

    private static void CompareSql<TItem>(EFRepositoryQuery<TItem> query, string expectedSql)
        where TItem : class
    {
        var sql = query.QueryString.Replace("\n", " ").Replace("\r", "");
        sql.Should().Be(expectedSql);
    }
}
