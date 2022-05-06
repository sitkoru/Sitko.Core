using System.Linq;
using FluentValidation.Results;

namespace Sitko.Core.Repository;

public record AddOrUpdateOperationResult<T, TId>(T Entity, ValidationFailure[] Errors, PropertyChange[] Changes)
    where T : IEntity<TId>
{
    public bool IsSuccess => Errors.Length == 0;

    public string ErrorsString => string.Join(" ", Errors.Select(e => e.ErrorMessage));
}
