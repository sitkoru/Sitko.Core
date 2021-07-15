using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sitko.Core.Search;

namespace Sitko.Core.Repository.Search
{
    public class SearchRepositoryFilter : BaseRepositoryFilter
    {
        private readonly IEnumerable<ISearchProvider> searchProviders;

        public SearchRepositoryFilter(IEnumerable<ISearchProvider> searchProviders) =>
            this.searchProviders = searchProviders;

        private ISearchProvider<TEntity, TEntityPk>? GetSearchProvider<TEntity, TEntityPk>() where TEntity : class
        {
            var provider = searchProviders.FirstOrDefault(s => s.CanProcess(typeof(TEntity)));
            return provider as ISearchProvider<TEntity, TEntityPk>;
        }

        private ISearchProvider? GetSearchProvider(Type entityType)
        {
            var provider = searchProviders.FirstOrDefault(s => s.CanProcess(entityType));
            return provider;
        }

        public override bool CanProcess(Type type) => GetSearchProvider(type) != null;

        public override async Task<bool> AfterSaveAsync<TEntity, TEntityPk>(TEntity item, bool isNew,
            PropertyChange[]? changes = null, CancellationToken cancellationToken = default)
        {
            var provider = GetSearchProvider<TEntity, TEntityPk>();
            if (provider != null)
            {
                await provider.AddOrUpdateEntityAsync(item, cancellationToken);
                return true;
            }

            return false;
        }
    }
}
