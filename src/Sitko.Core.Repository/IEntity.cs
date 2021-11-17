using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sitko.Core.Repository
{
    public interface IEntity<TEntityPk> : IEntity
    {
        TEntityPk Id { get; set; }
    }

    public interface IEntity
    {
        object? EntityId { get; }
    }

    public abstract class Entity<TEntityPk> : IEntity<TEntityPk>, IEquatable<Entity<TEntityPk>>
    {
        public virtual object? EntityId => Id;

        [Key]
#pragma warning disable 8618
        public virtual TEntityPk Id { get; set; }
#pragma warning restore 8618

        public override string ToString() => $"{GetType().Name} [Id: {Id}]";

        public bool Equals(Entity<TEntityPk>? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            return EqualityComparer<TEntityPk>.Default.Equals(Id, other.Id);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Entity<TEntityPk>)obj);
        }

        public override int GetHashCode() => EqualityComparer<TEntityPk>.Default.GetHashCode(Id);

        public static bool operator ==(Entity<TEntityPk>? lhs, Entity<TEntityPk>? rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }

            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Entity<TEntityPk>? lhs, Entity<TEntityPk>? rhs) => !(lhs == rhs);
    }

    public abstract record EntityRecord<TEntityPk> : IEntity<TEntityPk>
    {
        public object? EntityId => Id;

        [Key]
#pragma warning disable 8618
        public virtual TEntityPk Id { get; set; }
#pragma warning restore 8618
    }
}
