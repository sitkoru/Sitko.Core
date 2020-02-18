using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace Sitko.Core.Repository
{
    public interface IRepositoryFilter
    {
        bool CanProcess(Type type);

        Task<bool> BeforeValidateAsync<TEntity, TEntityPk>(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            bool isNew)
            where TEntity : class, IEntity<TEntityPk>;

        Task<bool> BeforeSaveAsync<TEntity, TEntityPk>(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            bool isNew,
            PropertyChange[] changes = null)
            where TEntity : class, IEntity<TEntityPk>;

        Task<bool> AfterSaveAsync<TEntity, TEntityPk>(TEntity item, bool isNew, PropertyChange[] changes = null)
            where TEntity : class, IEntity<TEntityPk>;
    }
}
