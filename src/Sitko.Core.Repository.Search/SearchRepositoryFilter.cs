using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitko.Core.Search;

namespace Sitko.Core.Repository.Search
{
    public class SearchRepositoryFilter : BaseRepositoryFilter
    {
        private readonly IEnumerable<ISearchProvider> _searchProviders;

        public SearchRepositoryFilter(IEnumerable<ISearchProvider> searchProviders)
        {
            _searchProviders = searchProviders;
        }

        private ISearchProvider<T>? GetSearchProvider<T>() where T : class
        {
            var provider = _searchProviders.FirstOrDefault(s => s.CanProcess(typeof(T)));
            return provider as ISearchProvider<T>;
        }

        private ISearchProvider? GetSearchProvider(Type entityType)
        {
            var provider = _searchProviders.FirstOrDefault(s => s.CanProcess(entityType));
            return provider;
        }

        public override bool CanProcess(Type type)
        {
            return GetSearchProvider(type) != null;
        }

        public override async Task<bool> AfterSaveAsync<TEntity, TEntityPk>(TEntity item, bool isNew,
            PropertyChange[]? changes = null)
        {
            var provider = GetSearchProvider<TEntity>();
            if (provider != null)
            {
                await provider.AddOrUpdateEntityAsync(item);
                return true;
            }

            return false;
        }
    }
}
