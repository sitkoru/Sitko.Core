using Microsoft.Extensions.Options;

namespace Sitko.Core.ClickHouse.Linq2db;

public abstract class BaseClickHouseDbContext(IHttpClientFactory httpClientFactory, IOptionsMonitor<ClickHouseModuleOptions> optionsMonitor, Dictionary<string, string>? settings = null, string? dbName = null)
    : LinqToDB.Data.DataConnection(options => options.SetClickHouseConnection(httpClientFactory, optionsMonitor.CurrentValue, settings, dbName));
