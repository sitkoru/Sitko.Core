using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Repository;

public class RepositoryFiltersManager
{
    private readonly IServiceProvider serviceProvider;

    public RepositoryFiltersManager(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;


    public async Task<bool> BeforeValidateAsync<T, TEntityPk>(T item,
        (bool isValid, IList<ValidationFailure> errors) validationResult,
        bool isNew, CancellationToken cancellationToken = default)
        where T : class, IEntity<TEntityPk>
    {
        var result = true;
        var filters = serviceProvider.GetServices<IRepositoryFilter>().ToArray();
        if (filters.Any())
        {
            foreach (var filter in filters)
            {
                if (!filter.CanProcess(typeof(T)))
                {
                    continue;
                }

                result = await filter.BeforeValidateAsync<T, TEntityPk>(item, validationResult, isNew,
                    cancellationToken);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    public async Task<bool> BeforeSaveAsync<T, TEntityPk>(T item,
        (bool isValid, IList<ValidationFailure> errors) validationResult,
        bool isNew,
        CancellationToken cancellationToken = default)
        where T : class, IEntity<TEntityPk>
    {
        var result = true;
        var filters = serviceProvider.GetServices<IRepositoryFilter>().ToArray();
        if (filters.Any())
        {
            foreach (var filter in filters)
            {
                if (!filter.CanProcess(typeof(T)))
                {
                    continue;
                }

                result = await filter.BeforeSaveAsync<T, TEntityPk>(item, validationResult, isNew, cancellationToken);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    public async Task<bool> AfterSaveAsync<T, TEntityPk>(T item, bool isNew, PropertyChange[]? changes = null,
        CancellationToken cancellationToken = default)
        where T : class, IEntity<TEntityPk>
    {
        var result = true;
        var filters = serviceProvider.GetServices<IRepositoryFilter>().ToArray();
        if (filters.Any())
        {
            foreach (var filter in filters)
            {
                if (!filter.CanProcess(typeof(T)))
                {
                    continue;
                }

                result = await filter.AfterSaveAsync<T, TEntityPk>(item, isNew, changes, cancellationToken);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
