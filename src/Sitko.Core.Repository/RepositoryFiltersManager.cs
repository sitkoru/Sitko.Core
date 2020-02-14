using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Repository
{
    public class RepositoryFiltersManager
    {
        private readonly IServiceProvider _serviceProvider;

        public RepositoryFiltersManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public async Task<bool> BeforeValidateAsync<T, TEntityPk>(T item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            bool isNew,
            PropertyChange[]? changes = null)
            where T : class, IEntity<TEntityPk>
        {
            var result = true;
            var filters = _serviceProvider.GetServices<IRepositoryFilter>().ToArray();
            if (filters.Any())
            {
                foreach (var filter in filters)
                {
                    if (!filter.CanProcess(typeof(T))) continue;
                    result = await filter.BeforeValidateAsync<T, TEntityPk>(item, validationResult, isNew, changes);
                }
            }

            return result;
        }

        public async Task<bool> BeforeSaveAsync<T, TEntityPk>(T item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            bool isNew,
            PropertyChange[]? changes = null)
            where T : class, IEntity<TEntityPk>
        {
            var result = true;
            var filters = _serviceProvider.GetServices<IRepositoryFilter>().ToArray();
            if (filters.Any())
            {
                foreach (var filter in filters)
                {
                    if (!filter.CanProcess(typeof(T))) continue;
                    result = await filter.BeforeSaveAsync<T, TEntityPk>(item, validationResult, isNew, changes);
                }
            }

            return result;
        }

        public async Task<bool> AfterSaveAsync<T, TEntityPk>(T item, bool isNew, PropertyChange[]? changes = null)
            where T : class, IEntity<TEntityPk>
        {
            var result = true;
            var filters = _serviceProvider.GetServices<IRepositoryFilter>().ToArray();
            if (filters.Any())
            {
                foreach (var filter in filters)
                {
                    if (!filter.CanProcess(typeof(T))) continue;
                    result = await filter.AfterSaveAsync<T, TEntityPk>(item, isNew, changes);
                }
            }

            return result;
        }
    }
}
