using System.Data;
using ClickHouse.Driver.ADO;

namespace Sitko.Core.ClickHouse;

public interface IClickHouseDbProvider
{
    public ClickHouseConnection GetConnection(Dictionary<string, string>? settings = null, string? dbName = null);
    public ClickHouseCommand GetCommand(string sql, IDbConnection connection);
}
