namespace Sitko.Core.Search;

public class BaseSearchModel
{
    public BaseSearchModel(string id, string title, string url, string content, DateTimeOffset date,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? highlight = null)
    {
        Highlight = highlight;
        Id = id;
        Title = title;
        Url = url;
        Content = content;
        Date = date;
    }

    public string Id { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Content { get; set; }
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>>? Highlight { get; set; }
}
