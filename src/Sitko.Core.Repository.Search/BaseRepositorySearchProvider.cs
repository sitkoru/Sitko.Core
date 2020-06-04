using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Search;

namespace Sitko.Core.Repository.Search
{
    public abstract class
        BaseRepositorySearchProvider<TEntity, TEntityPk, TSearchModel> : BaseSearchProvider<TEntity, TEntityPk,
            TSearchModel>
        where TSearchModel : BaseSearchModel where TEntity : class, IEntity<TEntityPk>
    {
        private readonly IRepository<TEntity, TEntityPk> _repository;

        protected BaseRepositorySearchProvider(
            ILogger<BaseSearchProvider<TEntity, TEntityPk, TSearchModel>> logger,
            IRepository<TEntity, TEntityPk> repository,
            ISearcher<TSearchModel>? searcher = null) : base(logger, searcher)
        {
            _repository = repository;
        }

        protected override Task<TEntity[]> GetEntitiesAsync(TSearchModel[] searchModels,
            CancellationToken cancellationToken = default)
        {
            var ids = searchModels.Select(s => ParseId(s.Id)).Distinct().ToArray();
            return _repository.GetByIdsAsync(ids, cancellationToken);
        }

        protected override string GetId(TEntity entity)
        {
            return entity.Id!.ToString();
        }
    }
}
