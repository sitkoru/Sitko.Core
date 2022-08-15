using FluentAssertions;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Repository.Tests;

public class ConditionsTests : BaseTest
{
    public ConditionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void EmptyGroup()
    {
        var group = new QueryContextConditionsGroup();
        group.Conditions.Count.Should().Be(0);
        group.Conditions.Add(new QueryContextCondition("test", QueryContextOperator.Contains, "123"));
        group.Conditions.Count.Should().Be(1);
    }
}
