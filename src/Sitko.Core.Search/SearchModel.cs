namespace Sitko.Core.Search;

public class BaseSearchModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Content { get; set; }
    public string[] Tags { get; set; }
}
