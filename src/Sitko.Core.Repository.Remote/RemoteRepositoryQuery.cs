using System.Linq.Expressions;

namespace Sitko.Core.Repository.Remote;

public class RemoteRepositoryQuery<TEntity> : BaseRepositoryQuery<TEntity> where TEntity : class
{
    private readonly List<IRemoteIncludableQuery> includableQueries = new();
    private readonly List<string> includesByName = new();
    private readonly List<Expression<Func<TEntity, object>>> orderByDescendingExpressions = new();
    private readonly List<Expression<Func<TEntity, object>>> orderByExpressions = new();
    private readonly List<(string propertyName, bool isDescending)> orderByStringExpressions = new();
    private readonly List<(string whereStr, object?[]? values)> whereByStringExpressions = new();
    private readonly List<Expression<Func<TEntity, bool>>> whereExpressions = new();
    private Expression? selectExpression;

    public RemoteRepositoryQuery()
    {
    }

    internal RemoteRepositoryQuery(RemoteRepositoryQuery<TEntity> source)
    {
        whereExpressions = source.whereExpressions;
        whereByStringExpressions = source.whereByStringExpressions;
        orderByExpressions = source.orderByExpressions;
        orderByDescendingExpressions = source.orderByExpressions;
        includesByName = source.includesByName;
        includableQueries = source.includableQueries;
        selectExpression = source.selectExpression;
    }


    public override IRepositoryQuery<TEntity> Include(string navigationPropertyPath)
    {
        includesByName.Add(navigationPropertyPath);
        return this;
    }

    public override IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> where)
    {
        whereExpressions.Add(where);
        return this;
    }

    public override IRepositoryQuery<TEntity> WhereByString(string whereJson)
    {
        whereByStringExpressions.Add((whereJson, null));
        return this;
    }

    public override IRepositoryQuery<TEntity> Where(Func<IQueryable<TEntity>, IQueryable<TEntity>> where) =>
        throw new NotImplementedException();

    public override IRepositoryQuery<TEntity> Where(string whereStr, object?[] values)
    {
        whereByStringExpressions.Add((whereStr, values));
        return this;
    }

    public override IRepositoryQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderBy)
    {
        orderByExpressions.Add(orderBy);
        return this;
    }

    public override IRepositoryQuery<TEntity> OrderBy(Expression<Func<TEntity, object>> orderBy)
    {
        orderByDescendingExpressions.Add(orderBy);
        return this;
    }

    public override IRepositoryQuery<TEntity> Order(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order) =>
        throw new NotImplementedException();

    public override IRepositoryQuery<TEntity> Configure(Action<IRepositoryQuery<TEntity>>? configureQuery = null)
    {
        configureQuery?.Invoke(this);

        return this;
    }

    public override async Task<IRepositoryQuery<TEntity>> ConfigureAsync(
        Func<IRepositoryQuery<TEntity>, Task>? configureQuery = null, CancellationToken cancellationToken = default)
    {
        if (configureQuery != null)
        {
            await configureQuery(this);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return this;
    }

    public override IIncludableRepositoryQuery<TEntity, TProperty> Include<TProperty>(
        Expression<Func<TEntity, TProperty>> navigationPropertyPath)
    {
        var includableQuery =
            new IncludableRemoteRepositoryQuery<TEntity, TProperty>(this, navigationPropertyPath);
        includableQueries.Add(includableQuery);
        return includableQuery;
    }

    protected override void ApplySort((string propertyName, bool isDescending) sortQuery) =>
        orderByStringExpressions.Add(sortQuery);

    public SerializedQuery<TEntity>
        Serialize()
    {
        var serializedQuery = new SerializedQuery<TEntity>()
            .AddWhereExpressions(whereExpressions)
            .AddWhereByStringExpressions(whereByStringExpressions)
            .AddOrderByExpressions(orderByExpressions)
            .AddOrderByDescendingExpressions(orderByDescendingExpressions)
            .AddOrderByStringExpressions(orderByStringExpressions)
            .AddIncludesByName(includesByName)
            .AddIncludes(includableQueries);
        if (selectExpression is not null)
        {
            serializedQuery.SetSelectExpression(selectExpression);
        }

        if (Limit > 0)
        {
            serializedQuery.SetLimit(Limit.Value);
        }

        if (Offset > 0)
        {
            serializedQuery.SetOffset(Offset.Value);
        }

        return serializedQuery;
    }

    public RemoteRepositoryQuery<TEntity> Select(Expression expression)
    {
        selectExpression = expression;
        return this;
    }
}

public class IncludableRemoteRepositoryQuery<TEntity, TProperty> : RemoteRepositoryQuery<TEntity>,
    IIncludableRepositoryQuery<TEntity, TProperty>, IRemoteIncludableQuery where TEntity : class
{
    private readonly Expression expression;
    private readonly RemoteRepositoryQuery<TEntity> source;

    internal IncludableRemoteRepositoryQuery(RemoteRepositoryQuery<TEntity> source, Expression expression) :
        base(source)
    {
        this.source = source;
        this.expression = expression;
    }

    private IChildRemoteIncludableQuery? Child { get; set; }

    public override IRepositoryQuery<TEntity> Take(int take)
    {
        source.Take(take);
        return this;
    }

    public override IRepositoryQuery<TEntity> Skip(int skip)
    {
        source.Skip(skip);
        return this;
    }

    public override IIncludableRepositoryQuery<TEntity, TProperty1> Include<TProperty1>(
        Expression<Func<TEntity, TProperty1>> navigationPropertyPath) => source.Include(navigationPropertyPath);

    public override IRepositoryQuery<TEntity> Include(string navigationPropertyPath) =>
        source.Include(navigationPropertyPath);

    public IIncludableRepositoryQuery<TEntity, TNextProperty> ThenIncludeFromEnumerableInternal<TNextProperty,
        TPreviousProperty>(
        Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath) =>
        IncludableRemoteRepositoryQuery<TEntity, TNextProperty>.CreateChild<TPreviousProperty>(this,
            navigationPropertyPath);

    public IIncludableRepositoryQuery<TEntity, TNextProperty> ThenIncludeFromSingleInternal<TNextProperty,
        TPreviousProperty>(
        Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath) =>
        IncludableRemoteRepositoryQuery<TEntity, TNextProperty>.CreateChild<TPreviousProperty>(this,
            navigationPropertyPath);

    public void SetChild<TPreviousProperty>(IRemoteIncludableQuery query) =>
        Child = new ChildRemoteIncludableQuery<TPreviousProperty>(query);

    public IInclude GetInclude(ExpressionSerializer serializer)
    {
        var include = new Include<TProperty>(serializer.Serialize(expression));
        if (Child is not null)
        {
            return Child.GetChildInclude(include, serializer);
        }

        return include;
    }

    public IInclude GetChildInclude<TPreviousProperty>(IInclude parentInclude, ExpressionSerializer serializer)
    {
        var include = new Include<TProperty, TPreviousProperty>(serializer.Serialize(expression), parentInclude);
        if (Child is not null)
        {
            return Child.GetChildInclude(include, serializer);
        }

        return include;
    }

    internal static IncludableRemoteRepositoryQuery<TEntity, TProperty> CreateChild<TPreviousProperty>(
        RemoteRepositoryQuery<TEntity> source, Expression expression)
    {
        var child = new IncludableRemoteRepositoryQuery<TEntity, TProperty>(source, expression);
        if (source is IRemoteIncludableQuery includableSource)
        {
            includableSource.SetChild<TPreviousProperty>(child);
        }

        return child;
    }
}

public interface IRemoteIncludableQuery
{
    public void SetChild<TPreviousProperty>(IRemoteIncludableQuery query);
    IInclude GetInclude(ExpressionSerializer serializer);
    IInclude GetChildInclude<TPreviousProperty>(IInclude parentInclude, ExpressionSerializer serializer);
}

public interface IChildRemoteIncludableQuery
{
    IInclude GetChildInclude(IInclude parentInclude, ExpressionSerializer serializer);
}

public record ChildRemoteIncludableQuery<TPreviousProperty>(IRemoteIncludableQuery Query) : IChildRemoteIncludableQuery
{
    public IInclude GetChildInclude(IInclude parentInclude, ExpressionSerializer serializer) =>
        Query.GetChildInclude<TPreviousProperty>(parentInclude, serializer);
}
