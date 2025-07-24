using FluentAssertions;
using Sitko.Core.App.Collections;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Tests;

public class EquatableDictionaryTests : BaseTest
{
    public EquatableDictionaryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void Compare()
    {
        var dictionary1 = new EquatableDictionary<long, Entity>();
        var dictionary2 = new EquatableDictionary<long, Entity>();
        var entity1 = new Entity(Guid.NewGuid(), 1, "123");
        var entity2 = new Entity(Guid.NewGuid(), 1, "456");
        var entity3 = new Entity(Guid.NewGuid(), 3, "456");
        var entity1_2 = new Entity(entity1.Id, 1, "123");
        var entity2_2 = new Entity(entity2.Id, 1, "456");
        entity1.Should().Be(entity1_2);
        entity2.Should().Be(entity2_2);
        dictionary1.Add(1, entity1);
        dictionary2.Add(1, entity1);
        dictionary1.Equals(dictionary2).Should().BeTrue();
        dictionary1.Add(2, entity2);
        dictionary2.Add(2, entity2);
        dictionary1.Equals(dictionary2).Should().BeTrue();
        dictionary1.Clear();
        dictionary1.Add(1, entity1);
        dictionary1.Add(2, entity2);
        dictionary1.Equals(dictionary2).Should().BeTrue();
        dictionary1.Remove(1);
        dictionary1.Equals(dictionary2).Should().BeFalse();
        dictionary1.Add(1, entity3);
        dictionary1.Equals(dictionary2).Should().BeFalse();
    }
}
