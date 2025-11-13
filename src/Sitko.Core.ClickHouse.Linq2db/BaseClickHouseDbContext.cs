using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ClickHouse.Linq2db;

public abstract class BaseClickHouseDbContext(
    IClickHouseDbProvider dbProvider,
    IOptionsMonitor<ClickHouseModuleOptions> optionsMonitor,
    Dictionary<string, string>? settings = null,
    string? dbName = null)
    : LinqToDB.Data.DataConnection(!optionsMonitor.CurrentValue.WithSsl
        ? new DataOptions().UseClickHouse(optionsMonitor.CurrentValue.GetConnectionString(settings, dbName),
            ClickHouseProvider.ClickHouseDriver)
        : new DataOptions().UseConnection(ClickHouseTools.GetDataProvider(ClickHouseProvider.ClickHouseDriver),
            dbProvider.GetConnection(settings, dbName)));
