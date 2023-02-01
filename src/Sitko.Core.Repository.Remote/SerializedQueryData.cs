using System.Linq.Expressions;
using Sitko.Core.App.Json;

namespace Sitko.Core.Repository.Remote;

public record SerializedQueryDataRequest(string SerializedQueryDataJson)
{
    public SerializedQueryData Data =>
        JsonHelper.DeserializeWithMetadata<SerializedQueryData>(SerializedQueryDataJson)!;
}

public record SerializedQueryData
{
    public List<string> Where { get; set; } = new();
    public List<WhereByString> WhereByString { get; set; } = new();
    public List<string> OrderBy { get; set; } = new();
    public List<string> OrderByDescending { get; set; } = new();
    public List<OrderByString> OrderByString { get; set; } = new();
    public List<string> IncludesByName { get; set; } = new();
    public List<IInclude> Includes { get; set; } = new();
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public string? SelectExpressionString { get; set; }
}

public record WhereByString(string WhereStr, object?[]? Values);

public record OrderByString(string PropertyName, bool IsDescending);

public interface IInclude
{
    IRepositoryQuery<TEntity> Apply<TEntity>(IRepositoryQuery<TEntity> query, ExpressionSerializer serializer)
        where TEntity : class;
}

public record Include<TProperty>(string Expression) : IInclude
{
    public IRepositoryQuery<TEntity> Apply<TEntity>(IRepositoryQuery<TEntity> query,
        ExpressionSerializer serializer)
        where TEntity : class
    {
        var ex = serializer.Deserialize<Expression<Func<TEntity, TProperty>>>(Expression);
        query = query.Include(ex);
        return query;
    }
}

public record Include<TProperty, TPreviousProperty>(string Expression, IInclude Previous) : IInclude
{
    public IRepositoryQuery<TEntity> Apply<TEntity>(IRepositoryQuery<TEntity> query, ExpressionSerializer serializer)
        where TEntity : class
    {
        var ex = serializer.Deserialize<Expression<Func<TPreviousProperty, TProperty>>>(Expression);
        query = Previous.Apply(query, serializer);
        if (query is IIncludableRepositoryQuery<TEntity, TPreviousProperty> includableQuery)
        {
            query = includableQuery.ThenInclude(ex);
        }
        else
        {
            if (query is IIncludableRepositoryQuery<TEntity, IEnumerable<TPreviousProperty>> enumerableIncludableQuery)
            {
                query = enumerableIncludableQuery.ThenInclude(ex);
            }
        }

        return query;
    }
}
