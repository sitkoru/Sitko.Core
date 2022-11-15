using System.Collections;

namespace Sitko.Core.Repository;

public record QueryContextCondition
{
    public QueryContextCondition(string property, QueryContextOperator @operator, object? value = null,
        Type? valueType = null)
    {
        Property = property;
        Operator = @operator;
        Value = value;
        ValueType = valueType ?? value?.GetType();
    }

    public string Property { get; }
    public QueryContextOperator Operator { get; }
    public object? Value { get; init; }
    public Type? ValueType { get; init; }

    public string GetExpression(int valueIndex) =>
        Operator switch
        {
            QueryContextOperator.Equal => $"{Property} == @{valueIndex}",
            QueryContextOperator.NotEqual => $"{Property} != @{valueIndex}",
            QueryContextOperator.Greater => $"{Property} > @{valueIndex}",
            QueryContextOperator.GreaterOrEqual => $"{Property} >= @{valueIndex}",
            QueryContextOperator.Less => $"{Property} < @{valueIndex}",
            QueryContextOperator.LessOrEqual => $"{Property} <= @{valueIndex}",
            QueryContextOperator.In => $"@{valueIndex}.Contains({Property})",
            QueryContextOperator.NotIn => $"!@{valueIndex}.Contains({Property})",
            QueryContextOperator.IsNull => $"{Property} == null",
            QueryContextOperator.NotNull => $"{Property} != null",
            QueryContextOperator.Contains => ValueType == typeof(string) ||
                                             typeof(IEnumerable).IsAssignableFrom(ValueType)
                ? $"{Property}.Contains(@{valueIndex})"
                : $"{Property}.ToString().Contains(@{valueIndex})",
            QueryContextOperator.NotContains => ValueType == typeof(string) ||
                                                typeof(IEnumerable).IsAssignableFrom(ValueType)
                ? $"!{Property}.Contains(@{valueIndex})"
                : $"!{Property}.ToString().Contains(@{valueIndex})",
            QueryContextOperator.ContainsCaseInsensitive => ValueType == typeof(string) ||
                                                            typeof(IEnumerable).IsAssignableFrom(ValueType)
                ? $"{Property}.ToLower().Contains(@{valueIndex}.ToLower())"
                : $"{Property}.ToString().ToLower().Contains(@{valueIndex})",
            QueryContextOperator.NotContainsCaseInsensitive when ValueType == typeof(string) ||
                                                                 typeof(IEnumerable).IsAssignableFrom(ValueType) =>
                $"!{Property}.ToLower().Contains(@{valueIndex}.ToLower())",
            QueryContextOperator.NotContainsCaseInsensitive =>
                $"!{Property}.ToString().ToLower().Contains(@{valueIndex})",
            QueryContextOperator.StartsWith => ValueType == typeof(string)
                ? $"{Property}.StartsWith(@{valueIndex})"
                : $"{Property}.ToString().StartsWith(@{valueIndex})",
            QueryContextOperator.NotStartsWith => ValueType == typeof(string)
                ? $"!{Property}.StartsWith(@{valueIndex})"
                : $"!{Property}.ToString().StartsWith(@{valueIndex})",
            QueryContextOperator.StartsWithCaseInsensitive => ValueType == typeof(string)
                ? $"{Property}.ToLower().StartsWith(@{valueIndex}.ToLower())"
                : $"{Property}.ToString().ToLower().StartsWith(@{valueIndex})",
            QueryContextOperator.NotStartsWithCaseInsensitive => ValueType == typeof(string)
                ? $"!{Property}.ToLower().StartsWith(@{valueIndex}.ToLower())"
                : $"!{Property}.ToString().ToLower().StartsWith(@{valueIndex}.ToLower())",
            QueryContextOperator.EndsWith => ValueType == typeof(string)
                ? $"{Property}.EndsWith(@{valueIndex})"
                : $"{Property}.ToString().EndsWith(@{valueIndex})",
            QueryContextOperator.NotEndsWith => ValueType == typeof(string)
                ? $"!{Property}.EndsWith(@{valueIndex})"
                : $"!{Property}.ToString().EndsWith(@{valueIndex})",
            QueryContextOperator.EndsWithCaseInsensitive => ValueType == typeof(string)
                ? $"{Property}.ToLower().EndsWith(@{valueIndex}.ToLower())"
                : $"{Property}.ToString().ToLower().EndsWith(@{valueIndex})",
            QueryContextOperator.NotEndsWithCaseInsensitive => ValueType == typeof(string)
                ? $"!{Property}.ToLower().EndsWith(@{valueIndex}.ToLower())"
                : $"!{Property}.ToString().ToLower().EndsWith(@{valueIndex})",
            _ => throw new InvalidOperationException($"Unknown operator {Operator}")
        };
}

