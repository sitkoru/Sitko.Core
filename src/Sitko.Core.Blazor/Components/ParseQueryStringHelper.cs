using System;
using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace Sitko.Core.Blazor.Components;

public static class ParseQueryStringHelper
{
    public static bool TryGetQueryString<T>(string queryString, string key, [NotNullWhen(true)] out T? value)
    {
        var valueFromQueryString = HttpUtility.ParseQueryString(queryString).Get(key);
        if (!string.IsNullOrEmpty(valueFromQueryString))
        {
            if (typeof(T).IsAssignableFrom(typeof(int)) && int.TryParse(valueFromQueryString, out var valueAsInt))
            {
                value = (T)(object)valueAsInt;
                return true;
            }

            if (typeof(T).IsAssignableFrom(typeof(string)))
            {
                value = (T)(object)valueFromQueryString;
                return true;
            }

            if (typeof(T).IsAssignableFrom(typeof(decimal)) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
            {
                value = (T)(object)valueAsDecimal;
                return true;
            }

            if (typeof(T).IsAssignableFrom(typeof(double)) && double.TryParse(valueFromQueryString, out var valueAsDouble))
            {
                value = (T)(object)valueAsDouble;
                return true;
            }

            if (typeof(T).IsAssignableFrom(typeof(Guid)) && Guid.TryParse(valueFromQueryString, out var valueAsGuid))
            {
                value = (T)(object)valueAsGuid;
                return true;
            }
        }

        value = default;
        return false;
    }
}
