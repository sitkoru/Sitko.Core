using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.Repository
{
    public interface IRepositoryQuery<TEntity> where TEntity : class
    {
        public int? Limit { get; }
        public int? Offset { get; }
        IRepositoryQuery<TEntity> Take(int take);
        IRepositoryQuery<TEntity> Skip(int skip);
        IRepositoryQuery<TEntity> Where(Expression<Func<TEntity, bool>> where);
        IRepositoryQuery<TEntity> Where(Func<IQueryable<TEntity>, IQueryable<TEntity>> where);
        IRepositoryQuery<TEntity> Where(string whereStr, object[] values);
        IRepositoryQuery<TEntity> Where(string property, object value);
        IRepositoryQuery<TEntity> Where(QueryContextCondition condition);
        IRepositoryQuery<TEntity> Where(QueryContextConditionsGroup conditionsGroup);
        IRepositoryQuery<TEntity> Where(IEnumerable<QueryContextConditionsGroup> conditionsGroups);
        IRepositoryQuery<TEntity> Like(string property, object value);
        IRepositoryQuery<TEntity> WhereByString(string whereJson);
        IRepositoryQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderBy);
        IRepositoryQuery<TEntity> OrderBy(Expression<Func<TEntity, object>> orderBy);
        IRepositoryQuery<TEntity> OrderBy(string property, bool isDescending);
        IRepositoryQuery<TEntity> OrderByString(string orderBy);
        IRepositoryQuery<TEntity> Order(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> order);
        IRepositoryQuery<TEntity> Configure(Action<IRepositoryQuery<TEntity>>? configureQuery = null);

        Task<IRepositoryQuery<TEntity>> ConfigureAsync(Func<IRepositoryQuery<TEntity>, Task>? configureQuery = null,
            CancellationToken cancellationToken = default);

        IRepositoryQuery<TEntity> Paginate(int page, int itemsPerPage);

        IIncludableRepositoryQuery<TEntity, TProperty> Include<TProperty>(
            Expression<Func<TEntity, TProperty>> navigationPropertyPath);
    }

    public interface IIncludableRepositoryQuery<TEntity, out TProperty> : IRepositoryQuery<TEntity>
        where TEntity : class
    {
    }
}
