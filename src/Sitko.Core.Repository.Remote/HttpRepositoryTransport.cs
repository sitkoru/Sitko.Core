﻿using System.Net;
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
                return Options.HttpClientFactory(httpClientFactory);
            }

            var client = httpClientFactory.CreateClient(nameof(HttpRepositoryTransport));
            client.BaseAddress = Options.RepositoryControllerApiRoute;
            return client;
        }
    }

    public async Task<TEntity?> GetAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = query.Serialize();
        return await PostRequestAsync<SerializedQueryDataRequest, TEntity?>($"/{typeof(TEntity).Name}" + "/Get",
            new SerializedQueryDataRequest(JsonHelper.SerializeWithMetadata(serialized.Data)), cancellationToken);
    }

    public async Task<int> CountAsync<TEntity>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var serialized = configureQuery.Serialize();
        return await PostRequestAsync<SerializedQueryDataRequest, int>($"/{typeof(TEntity).Name}" + "/Count",
            new SerializedQueryDataRequest(JsonHelper.SerializeWithMetadata(serialized.Data)),
            cancellationToken);
    }

    public async Task<TReturn> SumAsync<TEntity, TReturn>(RemoteRepositoryQuery<TEntity> configureQuery,
        SumType type,
        CancellationToken cancellationToken = default) where TEntity : class where TReturn : struct
    {
        var serialized = configureQuery.Serialize();
        return await PostRequestAsync<SerializedQueryDataRequest, TReturn>(
            $"/{typeof(TEntity).Name}" + "/Sum?type=" + type,
            new SerializedQueryDataRequest(JsonHelper.SerializeWithMetadata(serialized.Data)), cancellationToken);
    }

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>?> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull =>
        await PostRequestAsync<TEntity, AddOrUpdateOperationResult<TEntity, TEntityPk>>(
            $"/{typeof(TEntity).Name}" + "/Add", entity, cancellationToken);

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]?> AddAsync<TEntity, TEntityPk>(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull =>
        await PostRequestAsync<IEnumerable<TEntity>, AddOrUpdateOperationResult<TEntity, TEntityPk>[]>(
            $"/{typeof(TEntity).Name}" + "/Add", entities, cancellationToken);

    public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>?> UpdateAsync<TEntity, TEntityPk>(TEntity entity,
        TEntity? oldEntity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>
        where TEntityPk : notnull
    {
        var jsonEntity = new UpdateModel<TEntity>(entity, oldEntity);
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
        var result = await PostRequestAsync<SerializedQueryDataRequest, ListResult<TEntity>>(
            $"/{typeof(TEntity).Name}" + "/GetAll",
            new SerializedQueryDataRequest(JsonHelper.SerializeWithMetadata(serialized.Data)), cancellationToken);
        if (result is null)
        {
            return (Array.Empty<TEntity>(), 0);
        }

        return (result.Items, result.ItemsCount);
    }

    private async Task<TResponse?> PostRequestAsync<TRequest, TResponse>(string url, TRequest request,
        CancellationToken cancellationToken)
    {
        var requestJson = JsonHelper.SerializeWithMetadata(request!);
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

            if (result.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(result.ReasonPhrase);
            }
        }

        var response = await ReadResponseAsync(result, cancellationToken);
        return JsonHelper.DeserializeWithMetadata<TResponse>(response);
    }

    private static async Task<string> ReadResponseAsync(HttpResponseMessage responseMessage,
        CancellationToken cancellationToken)
    {
        var responseJson = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        return responseJson;
    }
}

public record UpdateModel<TEntity>(TEntity Entity, TEntity? OldEntity) where TEntity : class;

public record ListResult<TEntity>(TEntity[] Items, int ItemsCount);
