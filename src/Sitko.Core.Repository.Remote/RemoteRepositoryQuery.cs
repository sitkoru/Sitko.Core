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

    public override IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> @where)
    {
        whereExpressions.Add(where);
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

    public override IIncludableRepositoryQuery<TEntity, TProperty> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) => throw new NotImplementedException();

    protected override void ApplySort((string propertyName, bool isDescending) sortQuery)
    {
        //make dynamic linq or expression
        if (sortQuery.isDescending)
        {
            orderByDescendingExpressions.Add(entity => sortQuery.propertyName);
        }
    }

    public SerializedQuery<TEntity>
        Serialize() =>
        new SerializedQuery<TEntity>(whereExpressions,
            orderByExpressions,
            orderByDescendingExpressions, Limit, Offset);
}
