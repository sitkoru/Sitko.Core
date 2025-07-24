using Sitko.Core.App.Collections;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Tests;

public class OrderedCollectionTests : BaseTest
{
    public OrderedCollectionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void Positions()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        collection.AddItem(item1);
        Assert.Equal(0, item1.Position);
        var item2 = new TestItem();
        collection.AddItem(item2);
        Assert.Equal(1, item2.Position);
    }

    [Fact]
    public void Remove()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        var item2 = new TestItem();
        collection.SetItems(new[] { item1, item2 });
        Assert.Equal(1, item2.Position);
        collection.RemoveItem(item1);
        Assert.Single(collection);
        Assert.Equal(0, item2.Position);
    }

    [Fact]
    public void InsertAfter()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        var item2 = new TestItem();
        collection.SetItems(new[] { item1, item2 });
        Assert.Equal(1, item2.Position);
        var item3 = new TestItem();
        collection.AddItem(item3, item1);
        Assert.Equal(2, item2.Position);
        Assert.Equal(1, item3.Position);
    }

    [Fact]
    public void InsertBefore()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        var item2 = new TestItem();
        collection.SetItems(new[] { item1, item2 });
        Assert.Equal(1, item2.Position);
        var item3 = new TestItem();
        collection.AddItem(item3, item2, false);
        Assert.Equal(2, item2.Position);
        Assert.Equal(1, item3.Position);
    }

    [Fact]
    public void InsertMultipleAfter()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        var item2 = new TestItem();
        collection.SetItems(new[] { item1, item2 });
        Assert.Equal(1, item2.Position);
        var item3 = new TestItem();
        var item4 = new TestItem();
        collection.AddItems(new[] { item3, item4 }, item1);
        Assert.Equal(3, item2.Position);
        Assert.Equal(1, item3.Position);
        Assert.Equal(2, item4.Position);
    }

    [Fact]
    public void InsertMultipleBefore()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        var item2 = new TestItem();
        collection.SetItems(new[] { item1, item2 });
        Assert.Equal(1, item2.Position);
        var item3 = new TestItem();
        var item4 = new TestItem();
        collection.AddItems(new[] { item3, item4 }, item2, false);
        Assert.Equal(3, item2.Position);
        Assert.Equal(1, item3.Position);
        Assert.Equal(2, item4.Position);
    }

    [Fact]
    public void MoveDown()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        var item2 = new TestItem();
        collection.SetItems(new[] { item1, item2 });
        Assert.Equal(0, item1.Position);
        collection.MoveDown(item1);
        Assert.Equal(1, item1.Position);
        Assert.Equal(0, item2.Position);
    }

    [Fact]
    public void MoveUp()
    {
        var collection = new OrderedCollection<TestItem>();
        var item1 = new TestItem();
        var item2 = new TestItem();
        collection.SetItems(new[] { item1, item2 });
        Assert.Equal(1, item2.Position);
        collection.MoveUp(item2);
        Assert.Equal(0, item2.Position);
        Assert.Equal(1, item1.Position);
    }
}

public class TestItem : IOrdered
{
    public Guid Id { get; } = Guid.NewGuid();
    public int Position { get; set; }
}
