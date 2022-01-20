using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Results;

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
    protected abstract Task<IOperationResult> CanReadAsync(string path, HttpRequest request);
    protected abstract Task<IOperationResult> CanDeleteAsync(string? path, HttpRequest request);
    protected abstract Task<IOperationResult> CanListAsync(string? path, HttpRequest request);

    protected abstract Task<IOperationResult> CanUploadAsync(UploadStorageItem<TMetadata> uploadStorageItem,
        HttpRequest request);

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
        return Ok(JsonSerializer.Serialize(result));
    }
}

public class UploadStorageItem<TMetadata>
{
    public IFormFile? File { get; set; }
    public string? Path { get; set; }

    [ModelBinder(BinderType = typeof(UploadStorageItemBinder))]
    public TMetadata? Metadata { get; set; }

    public string? FileName { get; set; }
}

public class UploadStorageItemBinder : IModelBinder
{
    private readonly ILogger<UploadStorageItemBinder> logger;

    public UploadStorageItemBinder(ILogger<UploadStorageItemBinder> logger) => this.logger = logger;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var modelName = bindingContext.ModelName;

        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        try
        {
            var result = JsonSerializer.Deserialize(value, bindingContext.ModelType);
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
