using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;

namespace Sitko.Core.ClickHouse.Linq2db;

public static class ClickHouseExtensions
{
    public static DataOptions SetClickHouseConnection(this DataOptions options, ClickHouseModuleOptions chOptions,
        Dictionary<string, string>? settings = null, string? dbName = null)
    {
        if (!chOptions.WithSsl)
        {
            return options.UseClickHouse(chOptions.GetConnectionString(settings, dbName),
                ClickHouseProvider.ClickHouseDriver);
        }

        var connection = chOptions.GetDbConnection(settings, dbName);
        return options.UseConnection(ClickHouseTools.GetDataProvider(ClickHouseProvider.ClickHouseDriver), connection);
    }
}
