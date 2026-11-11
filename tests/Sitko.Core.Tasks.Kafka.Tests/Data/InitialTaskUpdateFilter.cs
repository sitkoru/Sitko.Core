using FluentValidation.Results;
using Sitko.Core.Repository;
using TaskStatus = Sitko.Core.Tasks.Data.Entities.TaskStatus;

namespace Sitko.Core.Tasks.Kafka.Tests.Data
{
    public sealed class InitialTaskUpdateFilter(TaskExecutionControl control) : BaseRepositoryFilter<TestTask>
    {
        public override Task<bool> BeforeValidateAsync<TEntity, TEntityPk>(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult, bool isNew,
            CancellationToken cancellationToken = default)
        {
            if (!isNew && item is TestTask { TaskStatus: TaskStatus.InProgress })
            {
                control.BeforeInitialUpdate?.Invoke();
            }

            return Task.FromResult(true);
        }
    }
}
