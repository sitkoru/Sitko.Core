using System.Data;
using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ClickHouse;

public class ClickHouseDbProvider(
    IOptionsMonitor<ClickHouseModuleOptions> optionsMonitor,
    IHttpClientFactory httpClientFactory) : IClickHouseDbProvider
{
    public ClickHouseConnection GetConnection(Dictionary<string, string>? settings = null,
        string? dbName = null) =>
        new(optionsMonitor.CurrentValue.GetConnectionString(settings, dbName),
            httpClientFactory, ClickHouseModule.HttpClientName);

    public ClickHouseCommand GetCommand(string sql, IDbConnection connection)
    {
        var command = new ClickHouseCommand((ClickHouseConnection)connection);
        command.CommandText = sql;
        return command;
    }
}
