using FluentAssertions;
using Sitko.Core.App.Collections;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.App.Tests;

public class ValueCollectionTests : BaseTest
{
    public ValueCollectionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void Add()
    {
        var collection = new ValueCollection<string>();
        collection.Count.Should().Be(0);
        collection.Add("test");
        collection.Count.Should().Be(1);
    }
}
