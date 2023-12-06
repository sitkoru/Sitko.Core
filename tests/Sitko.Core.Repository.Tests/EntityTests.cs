using Newtonsoft.Json;
using Sitko.Core.App.Collections;
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

    [Fact]
    public void EqualityRecord()
    {
        var fooRecord = new FooRecord
        {
            Id = Guid.NewGuid(),
            Test = "First",
            BarsList = new ValueCollection<BarRecord> { new BarRecord { Value = Guid.NewGuid().ToString() } },
            BarsDictionary = new EquatableDictionary<int, BarRecord>
            {
                {
                    1,
                    new BarRecord
                    {
                        Id = Guid.NewGuid(),
                        Value = Guid.NewGuid().ToString(),
                        Values = new EquatableDictionary<string, string>
                        {
                            { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
                        }
                    }
                }
            }
        };
        var fooEntityClone = JsonConvert.DeserializeObject<FooRecord>(JsonConvert.SerializeObject(fooRecord));
        Assert.Equal(fooRecord, fooEntityClone);

        fooEntityClone!.Test = "Clone";
        Assert.NotEqual(fooRecord, fooEntityClone);
    }
}

public class FooEntity : Entity<Guid>
{
    public string Test { get; set; } = "";
}

public class BarEntity : Entity<Guid>;

public record FooRecord : EntityRecord<Guid>
{
    public string Test { get; set; } = "";
    public ValueCollection<BarRecord> BarsList { get; set; } = new();
    public EquatableDictionary<int, BarRecord> BarsDictionary { get; set; } = new();
}

public record BarRecord : EntityRecord<Guid>
{
    public string Value { get; set; } = "";
    public EquatableDictionary<string, string> Values { get; set; } = new();
}
