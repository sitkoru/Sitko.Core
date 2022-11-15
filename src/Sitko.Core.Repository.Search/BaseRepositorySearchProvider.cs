using Microsoft.Extensions.Logging;
using Sitko.Core.Search;

namespace Sitko.Core.Repository.Search;

public abstract class
    BaseRepositorySearchProvider<TEntity, TEntityPk, TSearchModel> : BaseSearchProvider<TEntity, TEntityPk,
        TSearchModel>, IRepositorySearchProvider<TEntity>
    where TSearchModel : BaseSearchModel where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull
{
    private readonly IRepository<TEntity, TEntityPk> repository;

    protected BaseRepositorySearchProvider(
        ILogger<BaseRepositorySearchProvider<TEntity, TEntityPk, TSearchModel>> logger,
        IRepository<TEntity, TEntityPk> repository,
        ISearcher<TSearchModel>? searcher = null) : base(logger, searcher) =>
        this.repository = repository;

    public async Task ReindexAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var page = 0;
        while (true)
        {
            var (items, _) =
                await repository.GetAllAsync(q => q.Paginate(page, batchSize).OrderBy(e => e.Id),
                    cancellationToken);
            if (items.Length == 0)
            {
                break;
            }

            await AddOrUpdateEntitiesAsync(items, cancellationToken);
            page++;
        }
    }

    protected override Task<TEntity[]> GetEntitiesAsync(TSearchModel[] searchModels,
        CancellationToken cancellationToken = default)
    {
        var ids = searchModels.Select(s => ParseId(s.Id)).Distinct().ToArray();
        return repository.GetByIdsAsync(ids, cancellationToken);
    }

    protected override string GetId(TEntity entity) =>
        entity.Id.ToString() ?? throw new InvalidOperationException("Empty entity id");
}

