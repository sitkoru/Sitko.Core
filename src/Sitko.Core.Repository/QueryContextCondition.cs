using System;
using System.Collections;
using Newtonsoft.Json;

namespace Sitko.Core.Repository;

public class QueryContextCondition
{
    public QueryContextCondition(string property) => Property = property;

    [JsonConstructor]
    public QueryContextCondition(string property, QueryContextOperator @operator, object? value = null)
    {
        Property = property;
        Operator = @operator;
        Value = value;
        ValueType = value?.GetType();
    }

    public QueryContextCondition(string property, QueryContextOperator @operator, object? value,
        Type? valueType = null)
    {
        Property = property;
        Operator = @operator;
        Value = value;
        ValueType = valueType;
    }

    public string Property { get; set; }
    public QueryContextOperator Operator { get; set; }
    public object? Value { get; set; }
    public Type? ValueType { get; set; }

    public string GetExpression(int valueIndex)
    {
        switch (Operator)
        {
            case QueryContextOperator.Equal:
                return $"{Property} == @{valueIndex}";
            case QueryContextOperator.NotEqual:
                return $"{Property} != @{valueIndex}";
            case QueryContextOperator.Greater:
                return $"{Property} > @{valueIndex}";
            case QueryContextOperator.GreaterOrEqual:
                return $"{Property} >= @{valueIndex}";
            case QueryContextOperator.Less:
                return $"{Property} < @{valueIndex}";
            case QueryContextOperator.LessOrEqual:
                return $"{Property} <= @{valueIndex}";
            case QueryContextOperator.In:
                return $"@{valueIndex}.Contains({Property})";
            case QueryContextOperator.NotIn:
                return $"!@{valueIndex}.Contains({Property})";
            case QueryContextOperator.IsNull:
                return $"{Property} == null";
            case QueryContextOperator.NotNull:
                return $"{Property} != null";
            case QueryContextOperator.Contains:
                if (ValueType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(ValueType))
                {
                    return $"{Property}.Contains(@{valueIndex})";
                }

                return $"{Property}.ToString().Contains(@{valueIndex})";
            case QueryContextOperator.NotContains:
                if (ValueType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(ValueType))
                {
                    return $"!{Property}.Contains(@{valueIndex})";
                }

                return $"!{Property}.ToString().Contains(@{valueIndex})";
            case QueryContextOperator.ContainsCaseInsensitive:
                if (ValueType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(ValueType))
                {
                    return $"{Property}.ToLower().Contains(@{valueIndex}.ToLower())";
                }

                return $"{Property}.ToString().ToLower().Contains(@{valueIndex})";
            case QueryContextOperator.NotContainsCaseInsensitive:
                if (ValueType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(ValueType))
                {
                    return $"!{Property}.ToLower().Contains(@{valueIndex}.ToLower())";
                }

                return $"!{Property}.ToString().ToLower().Contains(@{valueIndex})";
            case QueryContextOperator.StartsWith:
                if (ValueType == typeof(string))
                {
                    return $"{Property}.StartsWith(@{valueIndex})";
                }

                return $"{Property}.ToString().StartsWith(@{valueIndex})";
            case QueryContextOperator.NotStartsWith:
                if (ValueType == typeof(string))
                {
                    return $"!{Property}.StartsWith(@{valueIndex})";
                }

                return $"!{Property}.ToString().StartsWith(@{valueIndex})";
            case QueryContextOperator.StartsWithCaseInsensitive:
                if (ValueType == typeof(string))
                {
                    return $"{Property}.ToLower().StartsWith(@{valueIndex}.ToLower())";
                }

                return $"{Property}.ToString().ToLower().StartsWith(@{valueIndex})";
            case QueryContextOperator.NotStartsWithCaseInsensitive:
                if (ValueType == typeof(string))
                {
                    return $"!{Property}.ToLower().StartsWith(@{valueIndex}.ToLower())";
                }

                return $"!{Property}.ToString().ToLower().StartsWith(@{valueIndex}.ToLower())";
            case QueryContextOperator.EndsWith:
                if (ValueType == typeof(string))
                {
                    return $"{Property}.EndsWith(@{valueIndex})";
                }

                return $"{Property}.ToString().EndsWith(@{valueIndex})";
            case QueryContextOperator.NotEndsWith:
                if (ValueType == typeof(string))
                {
                    return $"!{Property}.EndsWith(@{valueIndex})";
                }

                return $"!{Property}.ToString().EndsWith(@{valueIndex})";
            case QueryContextOperator.EndsWithCaseInsensitive:
                if (ValueType == typeof(string))
                {
                    return $"{Property}.ToLower().EndsWith(@{valueIndex}.ToLower())";
                }

                return $"{Property}.ToString().ToLower().EndsWith(@{valueIndex})";
            case QueryContextOperator.NotEndsWithCaseInsensitive:
                if (ValueType == typeof(string))
                {
                    return $"!{Property}.ToLower().EndsWith(@{valueIndex}.ToLower())";
                }

                return $"!{Property}.ToString().ToLower().EndsWith(@{valueIndex})";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
