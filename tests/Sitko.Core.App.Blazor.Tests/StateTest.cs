using FluentAssertions;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.App.Blazor.Tests;

public class StateTest : BaseTest<StateTestScope>
{
    public StateTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task SimpleModel()
    {
        var compressor = new JsonHelperStateCompressor();
        var data = new SimpleRecord { DateTime = DateTimeOffset.Now, Title = "Title", Summary = 10 };
        var bytes = await compressor.ToGzipAsync(data);
        bytes.Should().NotBeEmpty();
        var uncompressed = await compressor.FromGzipAsync<SimpleRecord>(bytes);
        uncompressed.Should().NotBeNull();
        uncompressed!.Id.Should().Be(data.Id);
        uncompressed.DateTime.Should().Be(data.DateTime);
        uncompressed.Title.Should().Be(data.Title);
        uncompressed.Summary.Should().Be(data.Summary);
    }

    [Fact]
    public async Task ListSimpleModel()
    {
        var compressor = new JsonHelperStateCompressor();
        var id1 = new Guid();
        var id2 = new Guid();
        var data = new List<SimpleRecord>
        {
            new() { Id = id1, DateTime = DateTimeOffset.Now, Title = "Title", Summary = 10 },
            new() { Id = id2, DateTime = DateTimeOffset.Now, Title = "Title", Summary = 20 }
        };
        var bytes = await compressor.ToGzipAsync(data);
        bytes.Should().NotBeEmpty();
        var uncompressed = await compressor.FromGzipAsync<List<SimpleRecord>>(bytes);
        uncompressed.Should().NotBeNull();
        uncompressed!.Count.Should().Be(data.Count);
        uncompressed.Should().Contain(r => r.Id == id1);
        uncompressed.Should().Contain(r => r.Id == id2);
    }

    [Fact]
    public async Task ModelWithAbstractRecords()
    {
        var record = new TestRecord();
        record.Blocks.Add(new TextBlock { Text = "Block1" });
        record.Blocks.Add(new TextBlock { Text = "Block2" });

        var scope = await GetScopeAsync();
        var stateCompressor = scope.GetService<IStateCompressor>();

        var gzip = await stateCompressor.ToGzipAsync(record);
        var unzipRecord = await stateCompressor.FromGzipAsync<TestRecord>(gzip);
        unzipRecord!.Blocks.Count.Should().Be(2);
    }

    [Fact]
    public async Task ListWithAbstractRecords()
    {
        var data = new List<TestRecord>
        {
            new()
            {
                Blocks = new List<TestBlock>
                {
                    new TextBlock { Text = "Block1" }, new TextBlock { Text = "Block2" }
                }
            },
            new() { Blocks = new List<TestBlock> { new TextBlock { Text = "Block3" } } }
        };

        var scope = await GetScopeAsync();
        var stateCompressor = scope.GetService<IStateCompressor>();

        var gzip = await stateCompressor.ToGzipAsync(data);
        gzip.Should().NotBeEmpty();
        var unzipRecord = await stateCompressor.FromGzipAsync<List<TestRecord>>(gzip);
        unzipRecord.Should().NotBeEmpty();
        unzipRecord!.Count.Should().Be(2);
    }
}

public record SimpleRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset DateTime { get; set; } = DateTimeOffset.Now;
    public string Title { get; set; } = string.Empty;
    public int Summary { get; set; }
}

public record TestRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<TestBlock> Blocks { get; set; } = new();
}

public abstract record TestBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public override string ToString() => GetType().Name;
}

public record TextBlock : TestBlock
{
    public string Text { get; set; } = string.Empty;
    public override string ToString() => Text;
}
