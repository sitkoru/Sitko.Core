using System.Data;
using System.Data.Common;

namespace Sitko.Core.ClickHouse;

public interface IClickHouseDbProvider
{
    public DbConnection GetConnection(Dictionary<string, string>? settings = null, string? dbName = null);
    public DbCommand GetCommand(string sql, IDbConnection connection);
}
