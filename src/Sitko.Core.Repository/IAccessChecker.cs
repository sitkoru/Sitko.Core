using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.Repository;

public interface IAccessChecker<in TEntity, TEntityPk> where TEntity : IEntity<TEntityPk>
{
    Task<bool> CheckAccessAsync(TEntity[] entities, CancellationToken cancellationToken = default);
}

public class EntityAccessDeniedException : Exception
{
    public EntityAccessDeniedException(IEntity entity) : base(
        $"Access to entity {entity} with id {entity.EntityId} is denied")
    {
    }
}
