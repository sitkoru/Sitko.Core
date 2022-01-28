using System.Text.Json;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Json;

namespace Sitko.Core.Repository.Remote;

public class HttpRepositoryTransport<TRepositoryOptions> : IRemoteRepositoryTransport  where TRepositoryOptions : RemoteRepositoryOptions
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptionsMonitor<TRepositoryOptions> optionsMonitor;

    public HttpRepositoryTransport(IHttpClientFactory httpClientFactory, IOptionsMonitor<TRepositoryOptions> optionsMonitor)
    {
        this.httpClientFactory = httpClientFactory;
        this.optionsMonitor = optionsMonitor;
    }

    protected TRepositoryOptions Options => optionsMonitor.CurrentValue;

    private HttpClient HttpClient
    {
        get
        {
            if (Options.HttpClientFactory is not null)
            {
                return Options.HttpClientFactory();
            }

            var client = httpClientFactory.CreateClient();
            client.BaseAddress = Options.RepositoryControllerApiRoute;
            return client;
        }
    }

    public async Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(SerializedQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var json = JsonSerializer.Serialize(query);
        var result = await HttpClient.GetAsync(json, cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        return JsonSerializer.Deserialize<(TEntity[], int)>(await result.Content.ReadAsStringAsync());
    }


    public Task<T?> SendAsync<T>(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<int> DeleteAsync<T>()
    {
        throw new NotImplementedException();
    }
}
