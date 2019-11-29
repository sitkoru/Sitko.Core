using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using Sitko.Core.Repository;

namespace Sitko.Core.Search
{
    public class SearchRepositoryFilter : IRepositoryFilter
    {
        private readonly IEnumerable<ISearchProvider> _searchProviders;

        public SearchRepositoryFilter(IEnumerable<ISearchProvider> searchProviders)
        {
            _searchProviders = searchProviders;
        }

        private ISearchProvider<T> GetSearchProvider<T>() where T : IEntity
        {
            var provider = _searchProviders.FirstOrDefault(s => s.CanProcess(typeof(T)));
            return provider as ISearchProvider<T>;
        }

        private ISearchProvider GetSearchProvider(Type entityType)
        {
            var provider = _searchProviders.FirstOrDefault(s => s.CanProcess(entityType));
            return provider;
        }

        public bool CanProcess(Type type)
        {
            return GetSearchProvider(type) != null;
        }

        public Task<bool> BeforeValidateAsync<TEntity, TEntityPk>(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            PropertyChange[] changes = null) where TEntity : class, IEntity<TEntityPk>
        {
            return Task.FromResult(true);
        }

        public Task<bool> BeforeSaveAsync<TEntity, TEntityPk>(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            PropertyChange[] changes = null) where TEntity : class, IEntity<TEntityPk>
        {
            return Task.FromResult(true);
        }

        public async Task<bool> AfterSaveAsync<TEntity, TEntityPk>(TEntity item, PropertyChange[] changes = null)
            where TEntity : class, IEntity<TEntityPk>
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
