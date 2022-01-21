using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Results;
using Sitko.Core.Storage.Internal;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Remote.Server;

public abstract class BaseRemoteStorageController<TStorageOptions, TMetadata> : Controller
    where TStorageOptions : StorageOptions
{
    private readonly IStorage<TStorageOptions> storage;

    protected BaseRemoteStorageController(IStorage<TStorageOptions> storage,
        ILogger<BaseRemoteStorageController<TStorageOptions, TMetadata>> logger)
    {
        Logger = logger;
        this.storage = storage;
    }

    protected ILogger<BaseRemoteStorageController<TStorageOptions, TMetadata>> Logger { get; }

    protected virtual Task<IOperationResult> CanUploadAsync(UploadStorageItem<TMetadata> uploadStorageItem,
        HttpRequest request) =>
        Task.FromResult<IOperationResult>(new OperationResult());

    protected virtual Task<IOperationResult> CanReadAsync(string path, HttpRequest request) =>
        Task.FromResult<IOperationResult>(new OperationResult());

    protected virtual Task<IOperationResult> CanDeleteAsync(string? path, HttpRequest request) =>
        Task.FromResult<IOperationResult>(new OperationResult());

    protected virtual Task<IOperationResult> CanListAsync(string? path, HttpRequest request) =>
        Task.FromResult<IOperationResult>(new OperationResult());

    protected virtual Task<IOperationResult> CanUpdateMetadataAsync(string? path, HttpRequest request) =>
        Task.FromResult<IOperationResult>(new OperationResult());

    [HttpGet]
    public async Task<ActionResult<RemoteStorageItem?>> Get(string path)
    {
        var canRead = await CanReadAsync(path, Request);
        if (!canRead.IsSuccess)
        {
            return BadRequest(canRead.ErrorMessage);
        }

        var item = await storage.GetAsync(path, HttpContext.RequestAborted);
        if (item is not null)
        {
            return Ok(JsonSerializer.Serialize(new RemoteStorageItem(item, storage.PublicUri(item))));
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<StorageItem>> Post([FromForm] UploadStorageItem<TMetadata> uploadData)
    {
        if (uploadData.File is null)
        {
            return BadRequest("No file stream");
        }

        if (string.IsNullOrEmpty(uploadData.Path))
        {
            return BadRequest("No file path");
        }

        if (string.IsNullOrEmpty(uploadData.FileName))
        {
            return BadRequest("No file name");
        }


        var canUpload = await CanUploadAsync(uploadData, Request);
        if (!canUpload.IsSuccess)
        {
            return BadRequest(canUpload.ErrorMessage);
        }

        var result = await storage.SaveAsync(uploadData.File.OpenReadStream(), uploadData.FileName, uploadData.Path,
            uploadData.Metadata,
            Request.HttpContext.RequestAborted);

        return Ok(JsonSerializer.Serialize(result));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string? path = null)
    {
        var canDelete = await CanDeleteAsync(path, Request);
        if (!canDelete.IsSuccess)
        {
            return BadRequest(canDelete.ErrorMessage);
        }

        if (path is null)
        {
            try { await storage.DeleteAllAsync(HttpContext.RequestAborted); }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        else
        {
            var isDeleted = await storage.DeleteAsync(path, HttpContext.RequestAborted);
            if (!isDeleted)
            {
                return BadRequest($"Can't delete item {path}");
            }
        }

        return Ok();
    }

    [HttpGet("List")]
    public async Task<IActionResult> List(string path)
    {
        var canList = await CanListAsync(path, Request);
        if (!canList.IsSuccess)
        {
            return BadRequest(canList.ErrorMessage);
        }

        var result = await storage.GetAllItemsAsync(path, HttpContext.RequestAborted);
        var storageMetadataProvider =
            HttpContext.RequestServices.GetService<IStorageMetadataProvider<TStorageOptions>>();
        if (storageMetadataProvider is not null)
        {
            var items = new List<StorageItemInfo>();
            foreach (var itemInfo in result)
            {
                var metadata = await storageMetadataProvider.GetMetadataAsync(itemInfo.Path);
                if (metadata is not null)
                {
                    items.Add(itemInfo with { Metadata = metadata });
                }
                else
                {
                    items.Add(itemInfo);
                }
            }

            return Ok(JsonSerializer.Serialize(items));
        }

        return Ok(JsonSerializer.Serialize(result));
    }

    [HttpPost("UpdateMetadata")]
    public async Task<IActionResult> UpdateMetadata(string path, [FromForm] string metadataJson)
    {
        var canUpdateMetadata = await CanUpdateMetadataAsync(path, Request);
        if (!canUpdateMetadata.IsSuccess)
        {
            return BadRequest(canUpdateMetadata.ErrorMessage);
        }

        var metadata = JsonSerializer.Deserialize<StorageItemMetadata>(metadataJson);
        if (metadata is null)
        {
            return BadRequest("Empty request");
        }

        if (string.IsNullOrEmpty(metadata.FileName))
        {
            return BadRequest("Empty file name");
        }

        var item = await storage.GetAsync(path, HttpContext.RequestAborted);

        if (item is null)
        {
            return NotFound();
        }

        var metadataObj = metadata.Data is not null ? JsonSerializer.Deserialize<TMetadata>(metadata.Data) : default;
        var result =
            await storage.UpdateMetaDataAsync(item, metadata.FileName, metadataObj, HttpContext.RequestAborted);
        return Ok(JsonSerializer.Serialize(result));
    }
}
