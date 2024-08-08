namespace Sitko.Core.Search;

public class BaseSearchModel
{
    public BaseSearchModel()
    {
    }

    public string Id { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Content { get; set; }
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Highlight { get; set; }
}
