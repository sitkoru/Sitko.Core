using Sitko.Core.Repository;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Data.Repository;

public interface ITaskRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : class, IBaseTask;

