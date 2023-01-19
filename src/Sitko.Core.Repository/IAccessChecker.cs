namespace Sitko.Core.Repository;

public interface IAccessChecker<in TEntity, TEntityPk> where TEntity : IEntity<TEntityPk> where TEntityPk : notnull
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

