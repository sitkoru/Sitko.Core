using Xunit;

namespace Sitko.Core.Repository.Tests;

public class EntityTests
{
    [Fact]
    public void Equality()
    {
        var fooEntity = new FooEntity { Id = Guid.NewGuid(), Test = "First" };
        var fooEntityClone = new FooEntity { Id = fooEntity.Id, Test = "First Clone" };
        var barEntity = new BarEntity { Id = fooEntity.Id };
        Assert.True(fooEntity == fooEntityClone);
        Assert.True(fooEntity.Equals(fooEntityClone));
        Assert.False(fooEntity == barEntity);
        Assert.False(fooEntity.Equals(barEntity));
    }
}

public class FooEntity : Entity<Guid>
{
    public string Test { get; set; } = "";
}

public class BarEntity : Entity<Guid>
{
}

