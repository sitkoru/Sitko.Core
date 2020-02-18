using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace Sitko.Core.Repository
{
    public abstract class BaseRepositoryFilter : IRepositoryFilter
    {
        public abstract bool CanProcess(Type type);

        public virtual Task<bool> BeforeValidateAsync<TEntity, TEntityPk>(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult, bool isNew)
            where TEntity : class, IEntity<TEntityPk>
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> BeforeSaveAsync<TEntity, TEntityPk>(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult, bool isNew,
            PropertyChange[] changes = null) where TEntity : class, IEntity<TEntityPk>
        {
            return Task.FromResult(true);
        }

        public virtual Task<bool> AfterSaveAsync<TEntity, TEntityPk>(TEntity item, bool isNew,
            PropertyChange[] changes = null) where TEntity : class, IEntity<TEntityPk>
        {
            return Task.FromResult(true);
        }
    }

    public abstract class BaseRepositoryFilter<TEntity> : BaseRepositoryFilter
    {
        public override bool CanProcess(Type type)
        {
            return typeof(TEntity).IsAssignableFrom(type);
        }
    }
}
