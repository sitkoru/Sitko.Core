namespace Sitko.Core.Repository
{
    using JetBrains.Annotations;

    [PublicAPI]
    public class RepositoryRecord<TEntity, TEntityPk> where TEntity : class, IEntity<TEntityPk>
    {
        public RepositoryRecord(TEntity item, bool isNew = true, PropertyChange[]? changes = null)
        {
            Item = item;
            IsNew = isNew;
            Changes = changes;
        }

        public TEntity Item { get; }
        public bool IsNew { get; }
        public PropertyChange[]? Changes { get; }
    }
}
