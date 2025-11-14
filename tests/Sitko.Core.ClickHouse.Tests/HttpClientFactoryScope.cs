using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.ClickHouse.Tests;

internal sealed class HttpClientFactoryScope : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    public HttpClientFactoryScope()
    {
        serviceProvider = new ServiceCollection()
            .AddClickhouseClient()
            .BuildServiceProvider();
        Factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    }

    public IHttpClientFactory Factory { get; }

    public void Dispose() => serviceProvider.Dispose();
}
