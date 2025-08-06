namespace Sitko.Core.Search;

public record SearchOptions
{
    public SearchType SearchType { get; init; } = SearchType.Morphology;
    public string[] Tags { get; init; } = [];
    public int TagsMinimumMatch { get; init; } = 1;
    public bool WithHighlight { get; init; }
    public int Limit { get; init; } = 20;
    public int Offset { get; init; }
}
