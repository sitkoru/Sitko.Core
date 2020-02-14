namespace Sitko.Core.Repository
{
    public class RepositoryRecord<TEntity, TEntityPk> where TEntity : class, IEntity<TEntityPk>
    {
        public TEntity Item { get; }
        public bool IsNew { get; }
        public PropertyChange[]? Changes { get; }
        public TEntity? OldItem { get; }


        public RepositoryRecord(TEntity item, bool isNew = true, PropertyChange[]? changes = null, TEntity? oldItem = null)
        {
            Item = item;
            IsNew = isNew;
            Changes = changes;
            OldItem = oldItem;
        }
    }
}
