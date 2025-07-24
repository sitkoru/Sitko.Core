using FluentAssertions;
using Sitko.Core.App.Collections;
using Sitko.Core.Xunit;
using Xunit;

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

    [Fact]
    public void Compare()
    {
        var collection1 = new ValueCollection<Entity>();
        var collection2 = new ValueCollection<Entity>();
        var entity1 = new Entity(Guid.NewGuid(), 1, "123");
        var entity2 = new Entity(Guid.NewGuid(), 1, "456");
        var entity3 = new Entity(Guid.NewGuid(), 3, "456");
        var entity1_2 = new Entity(entity1.Id, 1, "123");
        var entity2_2 = new Entity(entity2.Id, 1, "456");
        entity1.Should().Be(entity1_2);
        entity2.Should().Be(entity2_2);
        collection1.Add(entity1);
        collection2.Add(entity1);
        collection1.Equals(collection2).Should().BeTrue();
        collection1.Add(entity2);
        collection2.Add(entity2);
        collection1.Equals(collection2).Should().BeTrue();
        collection1.Clear();
        collection1.Add(entity2);
        collection1.Add(entity1);
        collection1.Equals(collection2).Should().BeTrue();
        collection1.Remove(entity1);
        collection1.Equals(collection2).Should().BeFalse();
        collection1.Add(entity3);
        collection1.Equals(collection2).Should().BeFalse();
    }
}

public record Entity(Guid Id, int Views, string Text);
