using System.ComponentModel.DataAnnotations;

namespace Sitko.Core.Repository
{
    public interface IEntity<TEntityPk> : IEntity
    {
        TEntityPk Id { get; set; }
    }

    public interface IEntity
    {
        object? GetId();
    }

    public abstract class Entity<TEntityPk> : IEntity<TEntityPk>
    {
        public virtual object? GetId() => Id;

        [Key]
#pragma warning disable 8618
        public virtual TEntityPk Id { get; set; }
#pragma warning restore 8618
    }
}
