using System.Linq.Expressions;
using Serialize.Linq.Serializers;

namespace Sitko.Core.Repository.Remote;

public record SerializedQuery<TEntity> where TEntity : class
{
    private readonly ExpressionSerializer serializer = new(new JsonSerializer());

    public SerializedQuery(SerializedQueryData? data = null) => Data = data ?? new SerializedQueryData();

    public SerializedQueryData Data { get; }

    public Expression<Func<TEntity, TValue>> SelectExpression<TValue>() => Data.SelectExpressionString is null
        ? throw new InvalidOperationException("Empty select expression")
        : (Expression<Func<TEntity, TValue>>)serializer.DeserializeText(Data.SelectExpressionString);

    public SerializedQuery<TEntity> SetSelectExpression(Expression selectExpression)
    {
        Data.SelectExpressionString = serializer.SerializeText(selectExpression);
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
        Data.Where.Add(serializer.SerializeText(expression));
        return this;
    }

    public SerializedQuery<TEntity> AddWhereByStringExpressions(List<string> whereByStringExpressions)
    {
        foreach (var whereByStringExpression in whereByStringExpressions)
        {
            AddWhereByStringExpression(whereByStringExpression);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddWhereByStringExpression(string whereByStringExpression)
    {
        Data.WhereByString.Add(whereByStringExpression);
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
        Data.OrderBy.Add(serializer.SerializeText(expression));
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
        Data.OrderByDescending.Add(serializer.SerializeText(expression));
        return this;
    }

    public SerializedQuery<TEntity> AddIncludes(IEnumerable<string> includes)
    {
        foreach (var include in includes)
        {
            AddInclude(include);
        }

        return this;
    }

    public SerializedQuery<TEntity> AddInclude(string include)
    {
        Data.Includes.Add(include);
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
            var ex = serializer.DeserializeText(expressionNode);
            query.Where((Expression<Func<TEntity, bool>>)ex);
        }

        foreach (var whereByString in Data.WhereByString)
        {
            query.WhereByString(whereByString);
        }

        foreach (var expressionNode in Data.OrderBy)
        {
            var ex = serializer.DeserializeText(expressionNode);
            query.OrderBy((Expression<Func<TEntity, object>>)ex);
        }

        foreach (var expressionNode in Data.OrderByDescending)
        {
            var ex = serializer.DeserializeText(expressionNode);
            query.OrderByDescending((Expression<Func<TEntity, object>>)ex);
        }

        foreach (var include in Data.Includes)
        {
            query.Include(include);
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
