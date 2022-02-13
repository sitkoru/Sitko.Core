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

    private static StringContent CreateJsonContent<TEntity>(TEntity content) where TEntity : class
    {
        var json = JsonSerializer.Serialize(content);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private async Task<TEntity> SendHttpRequestAsync<TEntity>(StringContent content, CancellationToken cancellationToken)
    {
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + $"/{typeof(TEntity).Name}" + "/get", content , cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var answer = JsonSerializer.Deserialize<TEntity>(await result.Content.ReadAsStringAsync());
        return answer;
    }

    public async Task<TEntity?> GetAsync<TEntity>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = configureQuery.Serialize();
        var content = CreateJsonContent(serialized);
        return await SendHttpRequestAsync<TEntity?>(content, cancellationToken);
    }

    public async Task<int> CountAsync<TEntity>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = configureQuery.Serialize();
        var content = CreateJsonContent(serialized);
        return await SendHttpRequestAsync<int>(content, cancellationToken);
    }

    public async Task<TReturn?> SumAsync<TEntity, TReturn>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class where TReturn : struct
    {
        var serialized = configureQuery.Serialize();
        var content = CreateJsonContent(serialized);
        return await SendHttpRequestAsync<TReturn?>(content, cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>
    {
        var content = CreateJsonContent(entity);
        return await SendHttpRequestAsync<AddOrUpdateOperationResult<TEntity, TEntityPk>>(content, cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync<TEntity, TEntityPk>(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk>
    {
        var content = CreateJsonContent(entities);
        return await SendHttpRequestAsync<AddOrUpdateOperationResult<TEntity, TEntityPk>[]>(content, cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync<TEntity, TEntityPk>(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>
    {
        var jsonEntity = new UpdateModel<TEntity>
        {
            Entity = entity,
            OldEntity = oldEntity
        };
        var content = CreateJsonContent(jsonEntity);
        return await SendHttpRequestAsync<AddOrUpdateOperationResult<TEntity, TEntityPk>>(content, cancellationToken);
    }

    public async Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var content = CreateJsonContent(entity);
        return await SendHttpRequestAsync<bool>(content, cancellationToken);
    }

    public async Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = query.Serialize();
        var content = CreateJsonContent(serialized);
        return await SendHttpRequestAsync<(TEntity[] items, int itemsCount)>(content, cancellationToken);
    }
}

public class UpdateModel<TEntity> where TEntity : class
{
    public TEntity Entity { get; set; }
    public TEntity? OldEntity { get; set; }
}
