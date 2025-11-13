using System.Net;
using ClickHouse.Driver.ADO;

namespace Sitko.Core.ClickHouse;

public static class ClickHouseExtensions
{
    public static ClickHouseConnection GetDbConnection(this ClickHouseModuleOptions options, Dictionary<string, string>? settings = null,
        string? dbName = null)
    {
        if (!options.WithSsl)
        {
            return new ClickHouseConnection(options.GetConnectionString(settings, dbName));
        }

        var httpClientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            //fix "session is locked", https://github.com/DarkWanderer/ClickHouse.Client/issues/236#issuecomment-2523069106
            MaxConnectionsPerServer = 1
        };
        var httpClient = new HttpClient(httpClientHandler);
        return new ClickHouseConnection(options.GetConnectionString(settings, dbName), httpClient);
    }
}
