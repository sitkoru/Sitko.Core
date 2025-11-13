using System.Data;
using System.Data.Common;
using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ClickHouse;

public class ClickHouseDbProvider(IOptionsMonitor<ClickHouseModuleOptions> optionsMonitor, IHttpClientFactory httpClientFactory) : IClickHouseDbProvider
{
    public DbConnection GetConnection(Dictionary<string, string>? settings = null, string? dbName = null) =>
        optionsMonitor.CurrentValue.GetDbConnection(httpClientFactory, settings, dbName);

    public DbCommand GetCommand(string sql, IDbConnection connection)
    {
        var command = new ClickHouseCommand((ClickHouseConnection)connection);
        command.CommandText = sql;
        return command;
    }
}
