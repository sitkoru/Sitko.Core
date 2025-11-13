using ClickHouse.Driver.ADO;

namespace Sitko.Core.ClickHouse;

public static class ClickHouseExtensions
{
    public static ClickHouseConnection GetDbConnection(this ClickHouseModuleOptions options,
        IHttpClientFactory httpClientFactory, Dictionary<string, string>? settings = null,
        string? dbName = null) =>
        !options.WithSsl
            ? new ClickHouseConnection(options.GetConnectionString(settings, dbName))
            : new ClickHouseConnection(options.GetConnectionString(settings, dbName), httpClientFactory, ClickHouseModule.HttpClientName);
}
