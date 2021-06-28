using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntRepositoryListComponent<TEntity, TEntityPk> : BaseAntListComponent<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new()
    {
        protected IRepository<TEntity, TEntityPk> Repository => GetService<IRepository<TEntity, TEntityPk>>();

        [Parameter] public Func<IRepositoryQuery<TEntity>, Task>? ConfigureQuery { get; set; }

        protected override Task<(TEntity[] items, int itemsCount)> GetDataAsync(LoadRequest<TEntity> request,
            CancellationToken cancellationToken = default)
        {
            return Repository.GetAllAsync(async query =>
            {
                if (ConfigureQuery is not null)
                {
                    await ConfigureQuery(query);
                }

                foreach (var filter in request.Filters)
                {
                    query = query.Where(filter);
                }

                await ConfigureQueryAsync(query);
                foreach (var sort in request.Sort)
                {
                    query = query.Order(sort);
                }

                query.Paginate(request.Page, PageSize);
            }, cancellationToken);
        }

        protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query)
        {
            return Task.CompletedTask;
        }
    }
}
