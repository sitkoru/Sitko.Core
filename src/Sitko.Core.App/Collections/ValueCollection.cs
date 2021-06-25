using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Sitko.Core.App.Collections
{
    public sealed class ValueCollection<T> : Collection<T>, IEquatable<ValueCollection<T>>, IFormattable
    {
        private readonly IEqualityComparer<T>? _equalityComparer;

        public ValueCollection() : this(new List<T>()) { }

        public ValueCollection(IEqualityComparer<T>? equalityComparer = null) : this(new List<T>(), equalityComparer)
        {
        }

        public ValueCollection(IList<T> list, IEqualityComparer<T>? equalityComparer = null) : base(list) =>
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

        public bool Equals(ValueCollection<T>? other)
        {
            Debug.Assert(_equalityComparer != null, "_equalityComparer != null");

            if (other is null) return false;

            if (ReferenceEquals(this, other)) return true;

            using var enumerator1 = this.GetEnumerator();
            using var enumerator2 = other.GetEnumerator();

            while (enumerator1.MoveNext())
                if (!enumerator2.MoveNext() || !(_equalityComparer!).Equals(enumerator1.Current, enumerator2.Current))
                    return false;

            return !enumerator2.MoveNext(); //both enumerations reached the end
        }

        public override bool Equals(object? obj) =>
            obj is { } && (ReferenceEquals(this, obj) || obj is ValueCollection<T> coll && Equals(coll));

        public override int GetHashCode() =>
            unchecked(Items.Aggregate(0,
                (current, element) => (current * 397) ^ (element is null ? 0 : _equalityComparer!.GetHashCode(element))
            ));

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            //TODO propose a meaningful format i.e. [,] or {;}

            return $"[{FormatValue(this, formatProvider)}]";
        }

        public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

        private static string? FormatValue(object? value, IFormatProvider? formatProvider) =>
            value switch
            {
                null => "∅",
                bool b => b ? "true" : "false",
                string s => $"\"{s}\"",
                char c => $"\'{c}\'",
                DateTime dt => dt.ToString("o", formatProvider),
                IFormattable @if => @if.ToString(null, formatProvider),
                IEnumerable ie => "[" +
                                  string.Join(", ", ie.Cast<object>().Select(e => FormatValue(e, formatProvider))) +
                                  "]",
                _ => value.ToString()
            };
    }
}
