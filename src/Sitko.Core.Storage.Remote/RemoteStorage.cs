using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Json;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Internal;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Remote;

public class RemoteStorage<TStorageOptions> : Storage<TStorageOptions>
    where TStorageOptions : StorageOptions, IRemoteStorageOptions
{
    private readonly HttpClient httpClient;
    private readonly IHttpClientFactory httpClientFactory;

    public RemoteStorage(HttpClient httpClient, IHttpClientFactory httpClientFactory,
        IOptionsMonitor<TStorageOptions> options,
        ILogger<RemoteStorage<TStorageOptions>> logger,
        IStorageCache<TStorageOptions>? cache = null,
        IStorageMetadataProvider<TStorageOptions>? metadataProvider = null) : base(options,
        logger,
        cache, metadataProvider)
    {
        httpClient.BaseAddress = Options.RemoteUrl;
        this.httpClient = httpClient;
        this.httpClientFactory = httpClientFactory;
    }

    protected override async Task<StorageItem> DoSaveAsync(UploadRequest uploadRequest,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "");
        using var content = new MultipartFormDataContent
        {
            // file
            { new StreamContent(uploadRequest.Stream), "file", Path.GetFileName(uploadRequest.FileName) },

            // payload
            { new StringContent(uploadRequest.Path), "path" },
            { new StringContent(uploadRequest.FileName), "fileName" }
        };

        if (uploadRequest.Metadata?.Data is not null)
        {
            content.Add(new StringContent(uploadRequest.Metadata?.Data), "metadata");
        }

        request.Content = content;

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            if (JsonSerializer.Deserialize<StorageItem>(json) is { } storageItem)
            {
                return storageItem;
            }

            throw new InvalidOperationException("Invalid server response");
        }

        throw new InvalidOperationException(response.ReasonPhrase);
    }

    protected override async Task<bool> DoDeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"?path={filePath}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        return false;
    }

    protected override async Task<bool> DoIsFileExistsAsync(StorageItem item,
        CancellationToken cancellationToken = default)
    {
        var response = await DoGetFileAsync(item.FilePath, cancellationToken);
        return response is not null;
    }

    protected override async Task DoDeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync("", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(response.ReasonPhrase);
        }
    }

    protected override async Task<StorageItemDownloadInfo?> DoGetFileAsync(string path,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetJsonAsync<RemoteStorageItem?>($"?path={path}");
        if (response is null)
        {
            return null;
        }

        var info = new StorageItemDownloadInfo(path, response.StorageItem.FileSize, response.StorageItem.LastModified,
            async () =>
            {
                var client = httpClientFactory.CreateClient();
                var fileResponse = await client.GetStreamAsync(response.PublicUri);
                return fileResponse;
            });
        info.SetMetadata(new StorageItemMetadata
        {
            FileName = response.StorageItem.FileName, Data = response.StorageItem.MetadataJson
        });

        return info;
    }

    protected override async Task<IEnumerable<StorageItemInfo>> GetAllItemsAsync(string path,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetJsonAsync<StorageItemInfo[]>($"List?path={path}") ?? Array.Empty<StorageItemInfo>();
}
