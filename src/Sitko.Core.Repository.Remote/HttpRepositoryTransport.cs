using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Json;

namespace Sitko.Core.Repository.Remote;

public class HttpRepositoryTransport : IRemoteRepositoryTransport
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptionsMonitor<HttpRepositoryTransportOptions> optionsMonitor;

    public HttpRepositoryTransport(IHttpClientFactory httpClientFactory,
        IOptionsMonitor<HttpRepositoryTransportOptions> optionsMonitor)
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

    public async Task<TReturn> SumAsync<TEntity, TReturn>(RemoteRepositoryQuery<TEntity> configureQuery,
        SumType type,
        CancellationToken cancellationToken = default) where TEntity : class where TReturn : struct
    {
        var serialized = configureQuery.Serialize();
        return await PostRequestAsync<SerializedQueryData, TReturn>(
            $"/{typeof(TEntity).Name}" + "/Sum?type=" + type, serialized.Data, cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk> =>
        await PostRequestAsync<TEntity, AddOrUpdateOperationResult<TEntity, TEntityPk>>(
            $"/{typeof(TEntity).Name}" + "/Add", entity, cancellationToken);

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync<TEntity, TEntityPk>(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> =>
        await PostRequestAsync<IEnumerable<TEntity>, AddOrUpdateOperationResult<TEntity, TEntityPk>[]>(
            $"/{typeof(TEntity).Name}" + "/Add", entities, cancellationToken);

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync<TEntity, TEntityPk>(TEntity entity,
        TEntity? oldEntity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>
    {
        var jsonEntity = new UpdateModel<TEntity> { Entity = entity, OldEntity = oldEntity };
        return await PostRequestAsync<UpdateModel<TEntity>, AddOrUpdateOperationResult<TEntity, TEntityPk>>(
            $"/{typeof(TEntity).Name}" + "/Update", jsonEntity, cancellationToken);
    }

    public async Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class =>
        await PostRequestAsync<TEntity, bool>($"/{typeof(TEntity).Name}" + "/Delete", entity, cancellationToken);

    public async Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = query.Serialize();
        var result = await PostRequestAsync<SerializedQueryData, ListResult<TEntity>>(
            $"/{typeof(TEntity).Name}" + "/GetAll", serialized.Data, cancellationToken);
        if (result is null)
        {
            return (Array.Empty<TEntity>(), 0);
        }

        return (result.Items, result.ItemsCount);
    }

    private async Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest request,
        CancellationToken cancellationToken)
    {
        var requestJson = JsonHelper.SerializeWithMetadata(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var result = await HttpClient.PostAsync(HttpClient.BaseAddress + url, content, cancellationToken);
        if (!result.IsSuccessStatusCode)
        {
            if (result.StatusCode == HttpStatusCode.BadRequest &&
                await ReadResponseAsync(result, cancellationToken) is
                    { } error)
            {
                throw new InvalidOperationException($"Remote error: {error}");
            }

            throw new InvalidOperationException(result.ReasonPhrase);
        }

        var response = await ReadResponseAsync(result, cancellationToken);
        return JsonHelper.DeserializeWithMetadata<TResponse>(response);
    }

    private async Task<string> ReadResponseAsync(HttpResponseMessage responseMessage,
        CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        var responseJson = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
#else
        var responseJson = await responseMessage.Content.ReadAsStringAsync();
#endif
        return responseJson;
    }
}

public class UpdateModel<TEntity> where TEntity : class
{
    public TEntity Entity { get; set; }
    public TEntity? OldEntity { get; set; }
}

public record ListResult<TEntity>(TEntity[] Items, int ItemsCount);
