using FluentValidation.Results;
using JetBrains.Annotations;

namespace Sitko.Core.Repository;

[PublicAPI]
public interface IRepositoryFilter
{
    bool CanProcess(Type type);

    Task<bool> BeforeValidateAsync<TEntity, TEntityPk>(TEntity item,
        (bool isValid, IList<ValidationFailure> errors) validationResult,
        bool isNew, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull;

    Task<bool> BeforeSaveAsync<TEntity, TEntityPk>(TEntity item,
        (bool isValid, IList<ValidationFailure> errors) validationResult,
        bool isNew,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull;

    Task<bool> AfterSaveAsync<TEntity, TEntityPk>(TEntity item, bool isNew, PropertyChange[]? changes = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull;
}

