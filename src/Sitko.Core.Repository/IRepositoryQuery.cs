using System;
using System.Linq.Expressions;
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

        IRepositoryQuery<TEntity> Where(string whereStr, object[] values);

        IRepositoryQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object>> orderBy);

        IRepositoryQuery<TEntity> OrderBy(Expression<Func<TEntity, object>> orderBy);

        IRepositoryQuery<TEntity> Configure(Action<IRepositoryQuery<TEntity>>? configureQuery = null);

        Task<IRepositoryQuery<TEntity>> ConfigureAsync(Func<IRepositoryQuery<TEntity>, Task>? configureQuery = null);

        IRepositoryQuery<TEntity> OrderByString(string orderBy);

        IRepositoryQuery<TEntity> WhereByString(string whereJson);
        IRepositoryQuery<TEntity> Paginate(int page, int itemsPerPage);
    }
}
