using FluentAssertions;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Blazor.Tests;

public class ParseTest : BaseTest
{
    public ParseTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void QueryStringParse()
    {
        const int defaultIntValue = 10;
        const string defaultStringValue = "testString";
        const double defaultDoubleValue = 11.75;
        var defaultGuidValue = Guid.NewGuid();

        var queryString =
            $"intValue={defaultIntValue}&stringValue={defaultStringValue}&doubleValue={defaultDoubleValue}&guidValue={defaultGuidValue}";

        ParseQueryStringHelper.TryGetQueryString<int>(queryString, "intValue", out var intValue);
        intValue.Should().Be(defaultIntValue);
        ParseQueryStringHelper.TryGetQueryString<int?>(queryString, "intValue", out var nullableIntValue);
        nullableIntValue.Should().Be(defaultIntValue);

        ParseQueryStringHelper.TryGetQueryString<string>(queryString, "stringValue", out var stringValue);
        stringValue.Should().Be(defaultStringValue);
        ParseQueryStringHelper.TryGetQueryString<string?>(queryString, "stringValue", out var nullableStringValue);
        nullableStringValue.Should().Be(defaultStringValue);

        ParseQueryStringHelper.TryGetQueryString<double>(queryString, "doubleValue", out var doubleValue);
        doubleValue.Should().Be(defaultDoubleValue);
        ParseQueryStringHelper.TryGetQueryString<double?>(queryString, "doubleValue", out var nullableDoubleValue);
        nullableDoubleValue.Should().Be(defaultDoubleValue);

        ParseQueryStringHelper.TryGetQueryString<Guid>(queryString, "guidValue", out var guidValue);
        guidValue.Should().Be(defaultGuidValue);
        ParseQueryStringHelper.TryGetQueryString<Guid?>(queryString, "guidValue", out var nullableGuidValue);
        nullableGuidValue.Should().Be(defaultGuidValue);

        ParseQueryStringHelper.TryGetQueryString<int>(queryString, "notFoundValue", out var nullableValue);
        nullableValue.Should().Be(default);

        ParseQueryStringHelper.TryGetQueryString<DateTime>(queryString, "intValue", out var notFoundType);
        notFoundType.Should().Be(default);
    }
}
