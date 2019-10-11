using System.ComponentModel.DataAnnotations;

namespace Sitko.Core.Repository
{
    public interface IEntity<TEntityPk> : IEntity
    {
        TEntityPk Id { get; set; }
    }

    public interface IEntity
    {
        object GetId();
    }

    public abstract class Entity<TEntityPk> : IEntity<TEntityPk>
    {
        public virtual object GetId() => Id;

        [Key]
        public virtual TEntityPk Id { get; set; }
    }
}
