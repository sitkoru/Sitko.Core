using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.ClickHouse.Linq2db.Tests;

internal sealed class HttpClientFactoryScope : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    public HttpClientFactoryScope(ClickHouseModuleOptions options)
    {
        serviceProvider = new ServiceCollection()
            .AddClickhouseClient(options)
            .BuildServiceProvider();
        Factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    }

    public IHttpClientFactory Factory { get; }

    public void Dispose() => serviceProvider.Dispose();
}
