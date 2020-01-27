using System;
using System.Collections;

namespace Sitko.Core.Repository
{
    public class QueryContextCondition
    {
        public string Property { get; set; }
        public QueryContextOperator Operator { get; set; }
        public object Value { get; set; }
        public Type ValueType { get; set; }

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
                case QueryContextOperator.Contains:
                    if (ValueType == typeof(string) || typeof(IEnumerable).IsAssignableFrom(ValueType))
                    {
                        return $"{Property}.ToLower().Contains(@{valueIndex})";
                    }

                    break;
                case QueryContextOperator.StartsWith:
                    if (ValueType == typeof(string))
                    {
                        return $"{Property}.ToLower().StartsWith(@{valueIndex})";
                    }

                    break;
                case QueryContextOperator.EndsWith:
                    if (ValueType == typeof(string))
                    {
                        return $"{Property}.ToLower().EndsWith(@{valueIndex})";
                    }

                    break;
                case QueryContextOperator.In:
                    return $"@{valueIndex}.Contains({Property})";
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }
    }
}