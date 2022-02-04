using System.Text;
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

    public async Task<TEntity?> GetAsync<TEntity>(SerializedQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var json = JsonSerializer.Serialize(query);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/get", content, cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<TEntity?>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<int> CountAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
    {
        var result = await HttpClient.GetAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/count", cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<int>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<int> CountAsync<TEntity>(SerializedQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var json = JsonSerializer.Serialize(configureQuery);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/count", content, cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<int>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<int> SumAsync<TEntity>(SerializedQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var json = JsonSerializer.Serialize(configureQuery);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/sum", content, cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<int>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>
    {
        var json = JsonSerializer.Serialize(entity);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/add", content, cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<AddOrUpdateOperationResult<TEntity, TEntityPk>>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync<TEntity, TEntityPk>(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk>
    {
        var json = JsonSerializer.Serialize(entities);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/AddRange", content, cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<AddOrUpdateOperationResult<TEntity, TEntityPk>[]>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<PropertyChange[]> UpdateAsync<TEntity>(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var jsonEntity = new UpdateModel<TEntity>
        {
            Entity = entity,
            OldEntity = oldEntity
        };
        var json = JsonSerializer.Serialize(jsonEntity);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/update", content);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<PropertyChange[]>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var json = JsonSerializer.Serialize(entity);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/delete", content,
            cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<bool>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(SerializedQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var json = JsonSerializer.Serialize(query);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/GetAll", content, cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<(TEntity[], int)>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var result = await HttpClient.GetAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/GetAll", cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<(TEntity[], int)>(await result.Content.ReadAsStringAsync());
        return answer;
    }
}

public class UpdateModel<TEntity> where TEntity : class
{
    public TEntity Entity { get; set; }
    public TEntity? OldEntity { get; set; }
}
