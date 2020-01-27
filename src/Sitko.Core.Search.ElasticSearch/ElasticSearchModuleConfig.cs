namespace Sitko.Core.Search.ElasticSearch
{
    public class ElasticSearchModuleConfig : SearchModuleConfig
    {
        public ElasticSearchModuleConfig(string prefix, string url, string login = "", string password = "",
            bool enableClientLogging = false)
        {
            Prefix = prefix;
            Url = url;
            Login = login;
            Password = password;
            EnableClientLogging = enableClientLogging;
        }

        public string Prefix { get; }
        public string Url { get; }
        public string Login { get; }
        public string Password { get; }
        public bool EnableClientLogging { get; }
    }
}
