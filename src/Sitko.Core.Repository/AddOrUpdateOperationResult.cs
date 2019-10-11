using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace Sitko.Core.Repository
{
    public class AddOrUpdateOperationResult<T, TId> where T : IEntity<TId>
    {
        public bool IsSuccess { get; }
        public T Entity { get; }
        public ValidationFailure[] Errors { get; }

        public AddOrUpdateOperationResult(T entity, IEnumerable<ValidationFailure> errors)
        {
            Entity = entity;
            var validationFailures = errors as ValidationFailure[] ?? errors.ToArray();
            Errors = validationFailures.ToArray();
            IsSuccess = !validationFailures.Any();
        }

        public string ErrorsString => string.Join(" ", Errors.Select(e => e.ErrorMessage));
    }
}
