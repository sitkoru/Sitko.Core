using System.Text.Json;
using Sitko.Core.App.Json;

namespace Sitko.Core.Repository.Remote;

public class HttpRepositoryTransport : IRemoteRepositoryTransport
{
    private readonly IHttpClientFactory httpClientFactory;
    public HttpRepositoryTransport(IHttpClientFactory httpClientFactory) => this.httpClientFactory = httpClientFactory;

    private HttpClient HttpClient
    {
        get
        {
            var client = httpClientFactory.CreateClient();
            //client.BaseAddress = ;
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
            throw new Exception();
        }

        return JsonSerializer.Deserialize<(TEntity[], int)>(result.Content.ToString());
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
