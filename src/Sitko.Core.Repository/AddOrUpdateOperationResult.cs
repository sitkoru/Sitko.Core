using FluentValidation.Results;

namespace Sitko.Core.Repository;

public record AddOrUpdateOperationResult<T, TEntityPk>(T Entity, ValidationFailure[] Errors, PropertyChange[] Changes)
    where T : IEntity<TEntityPk> where TEntityPk : notnull
{
    public bool IsSuccess => Errors.Length == 0;

    public string ErrorsString => string.Join(" ", Errors.Select(e => e.ErrorMessage));
}

