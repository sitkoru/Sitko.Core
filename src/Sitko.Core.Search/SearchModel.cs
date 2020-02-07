using System;

namespace Sitko.Core.Search
{
    public class BaseSearchModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Content { get; set; }

        public BaseSearchModel(string id, string title, string url, string content, DateTimeOffset date)
        {
            Id = id;
            Title = title;
            Url = url;
            Content = content;
            Date = date;
        }
    }
}
