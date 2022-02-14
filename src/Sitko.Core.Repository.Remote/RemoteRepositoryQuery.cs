using System.Linq.Expressions;

namespace Sitko.Core.Repository.Remote;

internal class RemoteRepositoryQuerySource<TEntity> where TEntity : class
{
    public RemoteRepositoryQuerySource(IQueryable<TEntity> query) => Query = query;
    internal IQueryable<TEntity> Query { get; set; }
}

public class RemoteRepositoryQuery<TEntity> : BaseRepositoryQuery<TEntity> where TEntity : class
{
    private List<Expression<Func<TEntity, bool>>> whereExpressions = new();
    private List<Expression<Func<TEntity, object>>> orderByExpressions = new();
    private List<Expression<Func<TEntity, object>>> orderByDescendingExpressions = new();
    private List<Expression<Func<TEntity, int>>> intSelectExpressions = new();
    private List<Expression<Func<TEntity, long>>> longSelectExpressions = new();
    private List<IRemoteIncludableQuery> _includableQueries;
    private List<string> _includes = new ();

    public RemoteRepositoryQuery()
    {
    }

    internal RemoteRepositoryQuery(RemoteRepositoryQuery<TEntity> source)
    {
       this.whereExpressions = source.whereExpressions;
       this.orderByExpressions = source.orderByExpressions;
       this.orderByDescendingExpressions = source.orderByExpressions;
       this.intSelectExpressions = source.intSelectExpressions;
       this.longSelectExpressions = source.longSelectExpressions;
    }

    internal List<string> IncludeProperties { get; } = new();

    public override IRepositoryQuery<TEntity> Include(string navigationPropertyPath)
    {
        IncludeProperties.Add(navigationPropertyPath);
        return this;
    }

    public override IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> @where)
    {
        whereExpressions.Add(where);
        return this;
    }

    public IRepositoryQuery<TEntity> Select(Expression<Func<TEntity, int>> intSelect)
    {
        intSelectExpressions.Add(intSelect);
        return this;
    }

    public IRepositoryQuery<TEntity> Select(Expression<Func<TEntity, long>> longSelect)
    {
        longSelectExpressions.Add(longSelect);
        return this;
    }

    public override IRepositoryQuery<TEntity> Where(Func<IQueryable<TEntity>, IQueryable<TEntity>> @where) => throw new NotImplementedException();

    public override IRepositoryQuery<TEntity> Where(string whereStr, object?[] values) => throw new NotImplementedException();

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

    public override IRepositoryQuery<TEntity> Order(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order) => throw new NotImplementedException();

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
        _includableQueries.Add(includableQuery);
        return includableQuery;
    }

    protected static string GetPropertyName<TExpressionEntity, TProperty>(Expression<Func<TExpressionEntity, TProperty>> navigationPropertyPath)
    {
        var expression =  (MemberExpression)navigationPropertyPath.Body;
        return expression.Member.Name;
    }


    protected override void ApplySort((string propertyName, bool isDescending) sortQuery) => throw new NotImplementedException();
    // {
    //     //make dynamic linq or expression
    //     if (sortQuery.isDescending)
    //     {
    //         orderByDescendingExpressions.Add(entity => sortQuery.propertyName);
    //     }
    // }

    public SerializedQuery<TEntity>
        Serialize()
    {
        foreach (var includableQuery in _includableQueries)
        {
            _includes.Add(includableQuery.GetFullPath());
        }
        return new SerializedQuery<TEntity>(whereExpressions,
            orderByExpressions,
            orderByDescendingExpressions, intSelectExpressions, longSelectExpressions,_includes, Limit, Offset);
    }

}

public class IncludableRemoteRepositoryQuery<TEntity, TProperty> : RemoteRepositoryQuery<TEntity>, IIncludableRepositoryQuery<TEntity, TProperty>, IRemoteIncludableQuery where TEntity : class
{

    private readonly RemoteRepositoryQuery<TEntity> source;
    private readonly string propertyName;

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
    public void SetChild(IRemoteIncludableQuery query)
    {
        child = query;
    }
    public string GetFullPath()
    {
        var path = propertyName;
        if (child is not null)
        {
            path += $".{child.GetFullPath()}";
        }

        return path;
    }


    internal static IncludableRemoteRepositoryQuery<TEntity, TNextProperty> ThenInclude<TPreviousProperty,
        TNextProperty>(
        IIncludableRepositoryQuery<TEntity, IEnumerable<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath)
    {
        var efQuery = (RemoteRepositoryQuery<TEntity>)source;
        var propertyName = GetPropertyName<TPreviousProperty, TNextProperty>(navigationPropertyPath);
        return new IncludableRemoteRepositoryQuery<TEntity, TNextProperty>(efQuery, propertyName);
    }

    internal static IncludableRemoteRepositoryQuery<TEntity, TNextProperty> ThenInclude<TPreviousProperty,
        TNextProperty>(
        IIncludableRepositoryQuery<TEntity, TPreviousProperty> source,
        Expression<Func<TPreviousProperty, TNextProperty>> navigationPropertyPath)
    {
        var efQuery = (RemoteRepositoryQuery<TEntity>)source;
        var propertyName = GetPropertyName(navigationPropertyPath);
        return new IncludableRemoteRepositoryQuery<TEntity, TNextProperty>(efQuery, propertyName);
    }

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
}

public interface IRemoteIncludableQuery
{
    public  string GetFullPath();
    public  void SetChild(IRemoteIncludableQuery query);
}
