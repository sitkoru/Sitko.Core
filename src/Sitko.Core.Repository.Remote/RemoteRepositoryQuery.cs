using System.Linq.Expressions;

namespace Sitko.Core.Repository.Remote;

internal class RemoteRepositoryQuerySource<TEntity> where TEntity : class
{
    public RemoteRepositoryQuerySource(IQueryable<TEntity> query) => Query = query;
    internal IQueryable<TEntity> Query { get; set; }
}

public class RemoteRepositoryQuery<TEntity> : BaseRepositoryQuery<TEntity> where TEntity : class
{
    private readonly List<IRemoteIncludableQuery> includableQueries = new();
    private readonly List<string> includes = new();
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
        includes = source.includes;
        includableQueries = source.includableQueries;
        selectExpression = source.selectExpression;
    }


    public override IRepositoryQuery<TEntity> Include(string navigationPropertyPath)
    {
        includes.Add(navigationPropertyPath);
        return this;
    }

    public override IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> where)
    {
        whereExpressions.Add(where);
        return this;
    }

    public override IRepositoryQuery<TEntity> WhereByString(string whereStr)
    {
        whereByStringExpressions.Add((whereStr, null));
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
        var propertyName = GetPropertyName(navigationPropertyPath);
        var includableQuery = new IncludableRemoteRepositoryQuery<TEntity, TProperty>(this, propertyName);
        includableQueries.Add(includableQuery);
        return includableQuery;
    }

    protected static string GetPropertyName<TExpressionEntity, TProperty>(
        Expression<Func<TExpressionEntity, TProperty>> navigationPropertyPath)
    {
        var expression = (MemberExpression)navigationPropertyPath.Body;
        return expression.Member.Name;
    }


    protected override void ApplySort((string propertyName, bool isDescending) sortQuery) =>
        orderByStringExpressions.Add(sortQuery);

    public SerializedQuery<TEntity>
        Serialize()
    {
        foreach (var includableQuery in includableQueries)
        {
            includes.Add(includableQuery.GetFullPath());
        }

        var serializedQuery = new SerializedQuery<TEntity>()
            .AddWhereExpressions(whereExpressions)
            .AddWhereByStringExpressions(whereByStringExpressions)
            .AddOrderByExpressions(orderByExpressions)
            .AddOrderByDescendingExpressions(orderByDescendingExpressions)
            .AddOrderByStringExpressions(orderByStringExpressions)
            .AddIncludes(includes);
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

    public RemoteRepositoryQuery<TEntity> Select(Expression selectExpression)
    {
        this.selectExpression = selectExpression;
        return this;
    }
}

public class IncludableRemoteRepositoryQuery<TEntity, TProperty> : RemoteRepositoryQuery<TEntity>,
    IIncludableRepositoryQuery<TEntity, TProperty>, IRemoteIncludableQuery where TEntity : class
{
    private readonly string propertyName;

    private readonly RemoteRepositoryQuery<TEntity> source;

    internal IncludableRemoteRepositoryQuery(RemoteRepositoryQuery<TEntity> source, string propertyName) : base(source)
    {
        this.source = source;
        this.propertyName = propertyName;
        if (source is IRemoteIncludableQuery includableSource)
        {
            includableSource.SetChild(this);
        }
    }

    private IRemoteIncludableQuery? child { get; set; }

    public override IRepositoryQuery<TEntity> Take(int take)
    {
        source.Take(take);
        return this;
    }

    public override IRepositoryQuery<TEntity> Skip(int take)
    {
        source.Skip(take);
        return this;
    }

    public override IIncludableRepositoryQuery<TEntity, TProperty1> Include<TProperty1>(
        Expression<Func<TEntity, TProperty1>> navigationPropertyPath) => source.Include(navigationPropertyPath);

    public override IRepositoryQuery<TEntity> Include(string navigationPropertyPath) =>
        source.Include(navigationPropertyPath);

    public void SetChild(IRemoteIncludableQuery query) => child = query;

    public string GetFullPath()
    {
        var path = propertyName;
        if (child is not null)
        {
            path += $".{child.GetFullPath()}";
        }

        return path;
    }

    public IIncludableRepositoryQuery<TEntity, TNextProperty> ThenIncludeFromEnumerableInternal<TNextProperty,
        TPreviousProperty>(
        Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath) =>
        new IncludableRemoteRepositoryQuery<TEntity, TNextProperty>(this, GetPropertyName(navigationPropertyPath));

    public IIncludableRepositoryQuery<TEntity, TNextProperty> ThenIncludeFromSingleInternal<TNextProperty,
        TPreviousProperty>(
        Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath) =>
        new IncludableRemoteRepositoryQuery<TEntity, TNextProperty>(this, GetPropertyName(navigationPropertyPath));
}

public interface IRemoteIncludableQuery
{
    public string GetFullPath();
    public void SetChild(IRemoteIncludableQuery query);
}
