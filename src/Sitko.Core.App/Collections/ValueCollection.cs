using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Sitko.Core.App.Collections
{
    public sealed class ValueCollection<T> : Collection<T>, IEquatable<ValueCollection<T>>, IFormattable
    {
        private readonly IEqualityComparer<T>? equalityComparer;

        public ValueCollection() : this(new List<T>()) { }

        public ValueCollection(IEqualityComparer<T>? equalityComparer = null) : this(new List<T>(), equalityComparer)
        {
        }

        public ValueCollection(IList<T> list, IEqualityComparer<T>? equalityComparer = null) : base(list) =>
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        public bool Equals(ValueCollection<T>? other)
        {
            Debug.Assert(equalityComparer != null, "_equalityComparer != null");

            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            using var enumerator1 = this.GetEnumerator();
            using var enumerator2 = other.GetEnumerator();

            while (enumerator1.MoveNext())
            {
                if (!enumerator2.MoveNext() || !equalityComparer!.Equals(enumerator1.Current, enumerator2.Current))
                {
                    return false;
                }
            }

            return !enumerator2.MoveNext(); //both enumerations reached the end
        }

        public override bool Equals(object? obj) =>
            obj is { } && (ReferenceEquals(this, obj) || (obj is ValueCollection<T> coll && Equals(coll)));

        public override int GetHashCode() =>
            unchecked(Items.Aggregate(0,
                (current, element) => (current * 397) ^ (element is null ? 0 : equalityComparer!.GetHashCode(element))
            ));

        public string ToString(string? format, IFormatProvider? formatProvider) => $"[{typeof(T).Name}[{Count}]]";

        public override string ToString() => ToString(null, CultureInfo.CurrentCulture);
    }
}
