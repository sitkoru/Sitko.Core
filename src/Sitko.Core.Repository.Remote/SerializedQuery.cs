using System.Linq.Expressions;
using Newtonsoft.Json;
using Remote.Linq;
using Remote.Linq.Newtonsoft.Json;

namespace Sitko.Core.Repository.Remote;

public class ExpressionSerializer
{
    private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings().ConfigureRemoteLinq();

    public string Serialize(Expression expression) =>
        JsonConvert.SerializeObject(expression.ToRemoteLinqExpression(), serializerSettings);

    public T Deserialize<T>(string json) where T : Expression
    {
        var exp = JsonConvert.DeserializeObject<global::Remote.Linq.Expressions.Expression>(json, serializerSettings)!;
        return (T)exp.ToLinqExpression();
    }
}

public record SerializedQuery<TEntity> where TEntity : class
{
    private readonly ExpressionSerializer serializer = new();

    public SerializedQuery(SerializedQueryData? data = null) => Data = data ?? new SerializedQueryData();

    public SerializedQueryData Data { get; }


    public Expression<Func<TEntity, TValue>> SelectExpression<TValue>() => Data.SelectExpressionString is null
        ? throw new InvalidOperationException("Empty select expression")
        : serializer.Deserialize<Expression<Func<TEntity, TValue>>>(Data.SelectExpressionString);

    public SerializedQuery<TEntity> SetSelectExpression(Expression selectExpression)
    {
        Data.SelectExpressionString =
            serializer.Serialize(selectExpression);
        return this;
    }


    public SerializedQuery<TEntity> AddWhereExpressions(IEnumerable<Expression<Func<TEntity, bool>>> expressions)
    {
        foreach (var expression in expressions)
        {
            AddWhereExpression(expression);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddWhereExpression(Expression<Func<TEntity, bool>> expression)
    {
        Data.Where.Add(serializer.Serialize(expression));
        return this;
    }

    public SerializedQuery<TEntity> AddWhereByStringExpressions(
        List<(string whereStr, object?[]? values)> whereByStringExpressions)
    {
        foreach (var whereByStringExpression in whereByStringExpressions)
        {
            AddWhereByStringExpression(whereByStringExpression);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddWhereByStringExpression(
        (string whereStr, object?[]? values) whereByStringExpression)
    {
        Data.WhereByString.Add(new WhereByString(whereByStringExpression.whereStr, whereByStringExpression.values));
        return this;
    }

    public SerializedQuery<TEntity> AddOrderByExpressions(IEnumerable<Expression<Func<TEntity, object>>> expressions)
    {
        foreach (var expression in expressions)
        {
            AddOrderByExpression(expression);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddOrderByExpression(Expression<Func<TEntity, object>> expression)
    {
        Data.OrderBy.Add(serializer.Serialize(expression));
        return this;
    }

    public SerializedQuery<TEntity> AddOrderByDescendingExpressions(
        IEnumerable<Expression<Func<TEntity, object>>> expressions)
    {
        foreach (var expression in expressions)
        {
            AddOrderByDescendingExpression(expression);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddOrderByDescendingExpression(Expression<Func<TEntity, object>> expression)
    {
        Data.OrderByDescending.Add(serializer.Serialize(expression));
        return this;
    }

    public SerializedQuery<TEntity> AddOrderByStringExpressions(
        IEnumerable<(string propertyName, bool isDescending)> orderByStringExpressions)
    {
        foreach (var expression in orderByStringExpressions)
        {
            Data.OrderByString.Add(new OrderByString(expression.propertyName, expression.isDescending));
        }

        return this;
    }

    public SerializedQuery<TEntity> AddIncludesByName(IEnumerable<string> includes)
    {
        foreach (var include in includes)
        {
            AddIncludeByName(include);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddIncludeByName(string include)
    {
        Data.IncludesByName.Add(include);
        return this;
    }

    public SerializedQuery<TEntity> AddIncludes(IEnumerable<IRemoteIncludableQuery> includes)
    {
        foreach (var include in includes)
        {
            AddInclude(include);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddInclude(IRemoteIncludableQuery include)
    {
        Data.Includes.Add(include.GetInclude(serializer));
        return this;
    }

    public SerializedQuery<TEntity> SetLimit(int limit)
    {
        Data.Limit = limit;
        return this;
    }

    public SerializedQuery<TEntity> SetOffset(int offset)
    {
        Data.Offset = offset;
        return this;
    }

    public void Apply(IRepositoryQuery<TEntity> query)
    {
        foreach (var expressionNode in Data.Where)
        {
            var ex = serializer.Deserialize<Expression<Func<TEntity, bool>>>(expressionNode);
            query.Where(ex);
        }

        foreach (var (whereStr, values) in Data.WhereByString)
        {
            if (values is not null)
            {
                query.Where(whereStr, values!);
            }
            else
            {
                query.WhereByString(whereStr);
            }
        }

        foreach (var expressionNode in Data.OrderBy)
        {
            var ex = serializer.Deserialize<Expression<Func<TEntity, object>>>(expressionNode);
            query.OrderBy(ex);
        }

        foreach (var expressionNode in Data.OrderByDescending)
        {
            var ex = serializer.Deserialize<Expression<Func<TEntity, object>>>(expressionNode);
            query.OrderByDescending(ex);
        }

        foreach (var (propertyName, isDescending) in Data.OrderByString)
        {
            query.OrderByString(isDescending ? $"-{propertyName}" : propertyName);
        }

        foreach (var include in Data.IncludesByName)
        {
            query.Include(include);
        }

        foreach (var include in Data.Includes)
        {
            query = include.Apply(query, serializer);
        }


        if (Data.Limit > 0)
        {
            query.Take(Data.Limit.Value);
        }

        if (Data.Offset > 0)
        {
            query.Skip(Data.Offset.Value);
        }
    }
}
