#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.App.Blazor.Tests;

public class StateTest : BaseTest<StateTestScope>
{
    public StateTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Unzip()
    {
        var record = new TestRecord();
        record.Blocks.Add(new TextBlock
        {
            Text = "Block1"
        });
        record.Blocks.Add(new TextBlock
        {
            Text = "Block2"
        });

        var scope = await GetScopeAsync();
        var stateCompressor = scope.GetService<IStateCompressor>();

        var gzip = await stateCompressor.ToGzipAsync(record);
        var unzipRecord = await stateCompressor.FromGzipAsync<TestRecord>(gzip);
        unzipRecord.Blocks.Count.Should().Be(2);
    }
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
    public override string ToString() => Text;
    public string Text { get; set; } = "";
}
#endif
