namespace Sitko.Core.Repository;

public enum QueryContextOperator
{
    Equal = 1,
    NotEqual = 2,
    Greater = 3,
    GreaterOrEqual = 4,
    Less = 5,
    LessOrEqual = 6,
    Contains = 7,
    StartsWith = 8,
    EndsWith = 9,
    In = 10,
    ContainsCaseInsensitive = 11,
    StartsWithCaseInsensitive = 12,
    EndsWithCaseInsensitive = 13,
    NotIn = 14,
    NotContains = 15,
    NotStartsWith = 16,
    NotEndsWith = 17,
    NotContainsCaseInsensitive = 18,
    NotStartsWithCaseInsensitive = 19,
    NotEndsWithCaseInsensitive = 20,
    IsNull = 21,
    NotNull = 22
}
