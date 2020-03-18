using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Search;

namespace Sitko.Core.Repository.Search
{
    public abstract class
        BaseRepositorySearchProvider<TEntity, TEntityPk, TSearchModel> : BaseSearchProvider<TEntity, TSearchModel>
        where TSearchModel : BaseSearchModel where TEntity : class, IEntity<TEntityPk>
    {
        private readonly IRepository<TEntity, TEntityPk> _repository;

        protected BaseRepositorySearchProvider(
            ILogger<BaseSearchProvider<TEntity, TSearchModel>> logger,
            IRepository<TEntity, TEntityPk> repository,
            ISearcher<TSearchModel>? searcher = null) : base(logger, searcher)
        {
            _repository = repository;
        }

        protected override Task<TEntity[]> GetEntitiesAsync(TSearchModel[] searchModels)
        {
            var ids = searchModels.Select(s => ParseId(s.Id)).Distinct().ToArray();
            return _repository.GetByIdsAsync(ids);
        }

        protected override string GetId(TEntity entity)
        {
            return entity.Id!.ToString();
        }

        protected abstract TEntityPk ParseId(string id);
    }
}
