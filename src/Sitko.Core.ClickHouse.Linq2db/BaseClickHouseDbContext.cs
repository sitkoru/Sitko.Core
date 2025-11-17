using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;

namespace Sitko.Core.ClickHouse.Linq2db;

public abstract class BaseClickHouseDbContext(
    IClickHouseDbProvider dbProvider,
    Dictionary<string, string>? settings = null,
    string? dbName = null)
    : LinqToDB.Data.DataConnection(new DataOptions().UseConnection(ClickHouseTools.GetDataProvider(ClickHouseProvider.ClickHouseDriver),
        dbProvider.GetConnection(settings, dbName)));
