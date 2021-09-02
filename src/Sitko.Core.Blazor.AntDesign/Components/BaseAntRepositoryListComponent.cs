using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Repository;
using System.Linq.Expressions;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class
        BaseAntRepositoryListComponent<TEntity, TEntityPk, TRepository> : BaseAntListComponent<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new() where TRepository : IRepository<TEntity, TEntityPk>
    {
        [Parameter] public Func<IRepositoryQuery<TEntity>, Task>? ConfigureQuery { get; set; }

        protected Task<TResult> ExecuteRepositoryOperation<TResult>(
            Func<TRepository, Task<TResult>> operation) =>
            ExecuteServiceOperation(operation);

        protected override Task<(TEntity[] items, int itemsCount)> GetDataAsync(LoadRequest<TEntity> request,
            CancellationToken cancellationToken = default) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.GetAllAsync(async query =>
                {
                    await DoConfigureQuery(request, query);
                    foreach (var sort in request.Sort)
                    {
                        query = query.Order(sort.Operation);
                    }

                    query.Paginate(request.Page, PageSize);
                }, cancellationToken);
            });

        private async Task DoConfigureQuery(LoadRequest<TEntity>? request, IRepositoryQuery<TEntity> query)
        {
            if (ConfigureQuery is not null)
            {
                await ConfigureQuery(query);
            }

            if (request is not null)
            {
                foreach (var filter in request.Filters)
                {
                    query = query.Where(filter.Operation);
                }
            }

            await ConfigureQueryAsync(query);
        }

        protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query) => Task.CompletedTask;

        public Task<int> SumAsync(Expression<Func<TEntity, int>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<int?> SumAsync(Expression<Func<TEntity, int?>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<long> SumAsync(Expression<Func<TEntity, long>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<long?> SumAsync(Expression<Func<TEntity, long?>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<double> SumAsync(Expression<Func<TEntity, double>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<double?> SumAsync(Expression<Func<TEntity, double?>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<float> SumAsync(Expression<Func<TEntity, float>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<float?> SumAsync(Expression<Func<TEntity, float?>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });

        public Task<decimal?> SumAsync(Expression<Func<TEntity, decimal?>> selector) =>
            ExecuteRepositoryOperation(repository =>
            {
                return repository.SumAsync(async query =>
                {
                    await DoConfigureQuery(LastRequest, query);
                }, selector);
            });
    }
}
