using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App.Collections;
using Sitko.Core.App.Helpers;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.MudBlazorComponents;

public abstract partial class MudFileUpload<TValue> : BaseComponent where TValue : class, new()
{
    private Expression<Func<TValue?>>? currentForValue;
    private FieldIdentifier? fieldIdentifier;
    protected abstract bool IsMultiple { get; }

    protected OrderedCollection<UploadedItem> Files { get; } = new();
    protected string InputId => $"fileInput-{ComponentId}";
    protected bool ShowOrdering => EnableOrdering && ItemsCount > 1;

    protected bool ShowUpload => (!IsMultiple && !Files.Any()) ||
                                 (IsMultiple && (MaxAllowedFiles < 1 || Files.Count() < MaxAllowedFiles));

    protected int? MaxFilesToUpload => MaxAllowedFiles > 1 ? MaxAllowedFiles - ItemsCount : null;
    protected int ItemsCount => Files.Count();
    protected UploadedItem? PreviewItem { get; set; }

    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IScriptInjector ScriptInjector { get; set; } = null!;

    [Parameter] public FileUploadDisplayMode DisplayMode { get; set; } = FileUploadDisplayMode.File;
    [Parameter] public string UploadPath { get; set; } = "";
    [Parameter] public Func<FileUploadRequest, FileStream, Task<object>>? GenerateMetadata { get; set; }
    [Parameter] public Func<TValue?, Task>? OnChange { get; set; }
    [Parameter] public virtual string ContentTypes { get; set; } = "";
    [Parameter] public long MaxFileSize { get; set; } = long.MaxValue;
    [Parameter] public int MaxAllowedFiles { get; set; }
    [Parameter] public string UploadText { get; set; } = "";
    [Parameter] public string PreviewText { get; set; } = "";
    [Parameter] public string DownloadText { get; set; } = "";
    [Parameter] public string RemoveText { get; set; } = "";
    [Parameter] public string LeftText { get; set; } = "";
    [Parameter] public string RightText { get; set; } = "";
    [Parameter] public RenderFragment<MudFileUpload<TValue>>? ChildContent { get; set; }
    [Parameter] public Func<MudFileUpload<TValue>, Task<ICollection<StorageItem>>>? CustomUpload { get; set; }
    [Parameter] public bool EnableOrdering { get; set; } = true;
    [Parameter] public Func<StorageItem, UploadedItemUrlType, string>? GeneratePreviewUrl { get; set; }
    [CascadingParameter] private EditContext? EditContext { get; set; }

    [EditorRequired] [Parameter] public IStorage Storage { get; set; } = null!;

    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<TValue?>>? For { get; set; }

    [Parameter] public TValue? Value { get; set; }


    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (string.IsNullOrEmpty(UploadText))
        {
            UploadText = LocalizationProvider["Upload"];
        }

        if (string.IsNullOrEmpty(PreviewText))
        {
            PreviewText = LocalizationProvider["Preview"];
        }

        if (string.IsNullOrEmpty(DownloadText))
        {
            DownloadText = LocalizationProvider["Download"];
        }

        if (string.IsNullOrEmpty(RemoveText))
        {
            RemoveText = LocalizationProvider["Remove"];
        }

        if (string.IsNullOrEmpty(LeftText))
        {
            LeftText = LocalizationProvider["Move left"];
        }

        if (string.IsNullOrEmpty(RightText))
        {
            RightText = LocalizationProvider["Move right"];
        }

        if (DisplayMode == FileUploadDisplayMode.Image && string.IsNullOrEmpty(ContentTypes))
        {
            ContentTypes = "image/jpeg,image/png,image/svg+xml";
        }
    }

    protected Task<object> GenerateMetadataAsync(FileUploadRequest request, FileStream stream)
    {
        if (GenerateMetadata is not null)
        {
            return GenerateMetadata(request, stream);
        }

        return Task.FromResult((object)null!);
    }

    protected void RemoveFile(UploadedItem file) => Files.RemoveItem(file);

    protected virtual UploadedItem CreateUploadedItem(StorageItem storageItem)
    {
        var urls = new Dictionary<UploadedItemUrlType, string>
        {
            { UploadedItemUrlType.Full, Storage.PublicUri(storageItem).ToString() }
        };
        if (DisplayMode == FileUploadDisplayMode.Image)
        {
            if (GeneratePreviewUrl is not null)
            {
                urls[UploadedItemUrlType.LargePreview] =
                    GeneratePreviewUrl(storageItem, UploadedItemUrlType.LargePreview);
                urls[UploadedItemUrlType.SmallPreview] =
                    GeneratePreviewUrl(storageItem, UploadedItemUrlType.SmallPreview);
            }
        }

        return new UploadedItem(storageItem, urls);
    }

    protected void PreviewFile(UploadedItem file) => PreviewItem = file;

    protected bool CanMoveBackward(UploadedItem file) => Files.CanMoveUp(file);
    protected bool CanMoveForward(UploadedItem file) => Files.CanMoveDown(file);

    protected void MoveBackward(UploadedItem file) => Files.MoveUp(file);

    protected void MoveForward(UploadedItem file) => Files.MoveDown(file);

    protected async Task UploadFilesAsync(InputFileChangeEventArgs arg)
    {
        await StartLoadingAsync();
        var files = arg.GetMultipleFiles();
        if (MaxAllowedFiles > 0 && files.Count > MaxAllowedFiles)
        {
            Logger.LogError("Max files count is {Count}", MaxAllowedFiles);
            Snackbar.Add(LocalizationProvider["Maximum of {0} files is allowed. Selected: {1}",
                MaxAllowedFiles,
                files.Count], Severity.Error);
            await StopLoadingAsync();
            return;
        }

        var results = new List<StorageItem>();
        foreach (var file in files)
        {
            try
            {
                if (MaxFileSize > 0 && file.Size > MaxFileSize)
                {
                    Logger.LogError("File {File} exceeds max file size of {Size}", file.Name, file.Size);
                    Snackbar.Add(LocalizationProvider[
                            "File {0} is too big — {1}. Files up to {2} are allowed.",
                            file.Name, FilesHelper.HumanSize(file.Size), FilesHelper.HumanSize(MaxFileSize)],
                        Severity.Error);
                    continue;
                }

                if (ContentTypes.Any() && !ContentTypes.Split(',').Contains(file.ContentType))
                {
                    Logger.LogError(
                        "File {File} content type {ContentType} is not in allowed list: {AllowedContentTypes}",
                        file.Name, file.ContentType, ContentTypes);
                    Snackbar.Add(LocalizationProvider[
                        "File {0} content type {1} is not in allowed list. Allowed types: {2}.",
                        file.Name, file.ContentType, ContentTypes], Severity.Error);
                    continue;
                }

                var path = Path.GetTempFileName();

                await using (FileStream fs = new(path, FileMode.Create))
                {
                    await file.OpenReadStream(MaxFileSize).CopyToAsync(fs);
                    var uploadInfo = new FileUploadRequest(file.Name, file.ContentType, file.Size, file.LastModified);
                    var result = await Storage.SaveAsync(fs, file.Name, UploadPath,
                        GenerateMetadataAsync(uploadInfo, fs));
                    results.Add(result);
                }

                File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.LogError("File: {Filename} Error: {Error}",
                    file.Name, ex.Message);
            }
        }

        if (results.Any())
        {
            Logger.LogDebug("Uploaded {Count} files", results.Count);
            Snackbar.Add(LocalizationProvider["{0} files were uploaded", results.Count], Severity.Success);

            await SetValueAsync(results);
        }

        await StopLoadingAsync();
    }

    public async Task ClearValueAsync() => await SetValueAsync(new List<StorageItem>());

    protected override void Initialize()
    {
        base.Initialize();
        if (Value is not null)
        {
            Files.SetItems(GetFiles(Value));
        }

        if (For != null && For != currentForValue)
        {
            fieldIdentifier = FieldIdentifier.Create(For);
            currentForValue = For;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await ScriptInjector.InjectAsync(CssInjectRequest.FromUrl("fileUpload",
                "_content/Sitko.Core.Blazor.MudBlazor/fileUpload.css"));
        }
    }

    protected abstract IEnumerable<UploadedItem> GetFiles(TValue value);

    private async Task NotifyChangesAsync(TValue? currentValue)
    {
        await ValueChanged.InvokeAsync(currentValue);
        if (OnChange is not null)
        {
            await OnChange(currentValue);
        }

        if (EditContext is not null && fieldIdentifier is not null)
        {
            EditContext.NotifyFieldChanged(fieldIdentifier.Value);
        }
    }

    public async Task SetValueAsync(ICollection<StorageItem> storageItems)
    {
        if (storageItems.Any())
        {
            var items = storageItems.Select(CreateUploadedItem);
            Files.AddItems(items);
        }
        else
        {
            Files.Clear();
        }

        await NotifyChangesAsync(GetValue());
    }

    protected abstract TValue? GetValue();
}

public class MudFileUpload : MudFileUpload<StorageItem>
{
    protected override bool IsMultiple => false;
    protected override IEnumerable<UploadedItem> GetFiles(StorageItem value) => new[] { CreateUploadedItem(value) };
    protected override StorageItem? GetValue() => Files.FirstOrDefault()?.StorageItem ?? null;
}

public enum FileUploadDisplayMode
{
    File = 1,
    Image = 2
}
