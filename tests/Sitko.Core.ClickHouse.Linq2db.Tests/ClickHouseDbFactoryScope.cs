using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ClickHouse.Linq2db.Tests;

internal sealed class ClickHouseDbFactoryScope : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    public ClickHouseDbFactoryScope(ClickHouseModuleOptions options)
    {
        serviceProvider = new ServiceCollection()
            .AddSingleton<IOptionsMonitor<ClickHouseModuleOptions>>(new StaticOptionsMonitor(options))
            .AddClickhouseClient()
            .AddScoped<IClickHouseDbProvider, ClickHouseDbProvider>()
            .BuildServiceProvider();

        Factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        ClickHouseDbProvider = serviceProvider.GetRequiredService<IClickHouseDbProvider>();
    }

    public IHttpClientFactory Factory { get; }
    public IClickHouseDbProvider ClickHouseDbProvider { get; }

    public void Dispose() => serviceProvider.Dispose();

    private sealed class StaticOptionsMonitor(ClickHouseModuleOptions current)
        : IOptionsMonitor<ClickHouseModuleOptions>
    {
        public ClickHouseModuleOptions CurrentValue => current;

        public ClickHouseModuleOptions Get(string? name) => current;

        public IDisposable OnChange(Action<ClickHouseModuleOptions, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
