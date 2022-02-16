using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Json;

namespace Sitko.Core.Repository.Remote;

public class HttpRepositoryTransport : IRemoteRepositoryTransport
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptionsMonitor<HttpRepositoryTransportOptions> optionsMonitor;

    public HttpRepositoryTransport(IHttpClientFactory httpClientFactory, IOptionsMonitor<HttpRepositoryTransportOptions> optionsMonitor)
    {
        this.httpClientFactory = httpClientFactory;
        this.optionsMonitor = optionsMonitor;
    }

    protected HttpRepositoryTransportOptions Options => optionsMonitor.CurrentValue;

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

    private async Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + url, content , cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<TResponse>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<TEntity?> GetAsync<TEntity>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = configureQuery.Serialize();
        return await PostRequestAsync<SerializedQueryData, TEntity?>($"/{typeof(TEntity).Name}" + "/Get",
            serialized.Data, cancellationToken);
    }

    public async Task<int> CountAsync<TEntity>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = configureQuery.Serialize();
        return await PostRequestAsync<SerializedQueryData, int>($"/{typeof(TEntity).Name}" + "/Count", serialized.Data,
            cancellationToken);
    }

    public async Task<TReturn?> SumAsync<TEntity, TReturn>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class where TReturn : struct
    {
        var serialized = configureQuery.Serialize();
        return await PostRequestAsync<SerializedQuery<TEntity>,TReturn?>($"/{typeof(TEntity).Name}"+"/Sum"+nameof(TReturn), serialized, cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>
    {
        return await PostRequestAsync<TEntity, AddOrUpdateOperationResult<TEntity, TEntityPk>>($"/{typeof(TEntity).Name}"+"/Add",entity, cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync<TEntity, TEntityPk>(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk>
    {
        return await PostRequestAsync<IEnumerable<TEntity>, AddOrUpdateOperationResult<TEntity, TEntityPk>[]>($"/{typeof(TEntity).Name}"+"/Add", entities, cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync<TEntity, TEntityPk>(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>
    {
        var jsonEntity = new UpdateModel<TEntity>
        {
            Entity = entity,
            OldEntity = oldEntity
        };
        return await PostRequestAsync<UpdateModel<TEntity>, AddOrUpdateOperationResult<TEntity, TEntityPk>>($"/{typeof(TEntity).Name}"+"/Update", jsonEntity, cancellationToken);
    }

    public async Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return await PostRequestAsync<TEntity, bool>($"/{typeof(TEntity).Name}"+"/Delete", entity, cancellationToken);
    }

    public async Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = query.Serialize();
        return await PostRequestAsync<SerializedQuery<TEntity>, (TEntity[] items, int itemsCount)>($"/{typeof(TEntity).Name}"+"/GetAll", serialized, cancellationToken);
    }
}

public class UpdateModel<TEntity> where TEntity : class
{
    public TEntity Entity { get; set; }
    public TEntity? OldEntity { get; set; }
}
