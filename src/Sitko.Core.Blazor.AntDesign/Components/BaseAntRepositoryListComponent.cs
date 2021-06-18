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

        protected override Task<(TEntity[] items, int itemsCount)> GetDataAsync(string orderBy,
            int page = 1, CancellationToken cancellationToken = default)
        {
            return Repository.GetAllAsync(async query =>
            {
                if (ConfigureQuery is not null)
                {
                    await ConfigureQuery(query);
                }

                await ConfigureQueryAsync(query);
                query.OrderByString(orderBy).Paginate(page, PageSize);
            }, cancellationToken);
        }

        protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query)
        {
            return Task.CompletedTask;
        }
    }
}
