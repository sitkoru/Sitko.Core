using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.App.Collections;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components;

public abstract class BaseAntStorageInput<TValue> : InputBase<TValue>, IBaseComponent
    where TValue : class, new()
{
    protected OrderedCollection<UploadedItem> Files { get; } = new();
    protected UploadedItem? PreviewItem { get; set; }
    protected string ListClass => Mode == AntStorageInputMode.File ? "picture" : "picture-card";

    [Parameter] public string UploadPath { get; set; } = "";
    [Parameter] public Func<FileUploadRequest, FileStream, Task<object>>? GenerateMetadata { get; set; }
    [Parameter] public Func<TValue?, Task>? OnChange { get; set; }
    [Parameter] public virtual string ContentTypes { get; set; } = "";
    [Parameter] public long MaxFileSize { get; set; }
    protected virtual int? MaxAllowedFiles { get; set; }
    [Parameter] public string UploadText { get; set; } = "";
    [Parameter] public string PreviewText { get; set; } = "";
    [Parameter] public string DownloadText { get; set; } = "";
    [Parameter] public string RemoveText { get; set; } = "";
    [EditorRequired]
    [Parameter]
    public IStorage Storage { get; set; } = null!;

    [Parameter] public bool EnableOrdering { get; set; } = true;
    [Parameter] public virtual AntStorageInputMode Mode { get; set; } = AntStorageInputMode.File;
    [Parameter] public Func<StorageItem, UploadedItemUrlType, string>? GeneratePreviewUrl { get; set; }
    [Parameter] public bool Avatar { get; set; }

    protected string AvatarSize => Size switch
    {
        "large" => "238",
        "small" => "46",
        _ => "86"
    };

    [Parameter] public string Size { get; set; } = "default";
    [Parameter] public string LeftText { get; set; } = "";
    [Parameter] public string RightText { get; set; } = "";
    [Parameter] public RenderFragment<BaseAntStorageInput<TValue>>? CustomUploadButton { get; set; }
    [Parameter] public Func<BaseAntStorageInput<TValue>, Task<TValue>>? CustomUpload { get; set; }
    [PublicAPI] protected int ItemsCount => Files.Count();
    protected bool ShowOrdering => EnableOrdering && ItemsCount > 1;
    protected IBaseFileInputComponent? FileInput { get; set; }
    protected bool IsSpinning => FileInput?.IsLoading ?? false;

    protected bool ShowUpload => MaxAllowedFiles is null || MaxAllowedFiles < 1 || ItemsCount < MaxAllowedFiles;
    protected int? MaxFilesToUpload => MaxAllowedFiles is not null ? MaxAllowedFiles - ItemsCount : null;

    protected List<StorageItem>? Items
    {
        get => Files.Select(f => f.StorageItem).ToList();
        set
        {
            if (value is not null)
            {
                Files.AddItems(value.Select(CreateUploadedItem));
            }
            else
            {
                Files.SetItems(Array.Empty<UploadedItem>());
            }

            UpdateCurrentValue();
        }
    }


    [Inject]
    protected ILocalizationProvider<AntStorageInput<TValue>> LocalizationProvider { get; set; } =
        null!;

    public Task NotifyStateChangeAsync() => InvokeAsync(StateHasChanged);

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

        if (CurrentValue is not null)
        {
            var value = ParseCurrentValue(CurrentValue);
            Files.SetItems(value);
        }
    }

    protected abstract IEnumerable<UploadedItem> ParseCurrentValue(TValue currentValue);

    protected Task OnChangeAsync()
    {
        if (OnChange is not null)
        {
            return OnChange(CurrentValue);
        }

        return Task.CompletedTask;
    }

    protected Task<object> GenerateMetadataAsync(FileUploadRequest request, FileStream stream)
    {
        if (GenerateMetadata is not null)
        {
            return GenerateMetadata(request, stream);
        }

        return Task.FromResult((object)null!);
    }

    protected void RemoveFile(UploadedItem file)
    {
        Files.RemoveItem(file);
        UpdateCurrentValue();
    }

    protected void UpdateCurrentValue()
    {
        var value = UpdateCurrentValue(Files.ToList());
        CurrentValue = value!;
    }

    protected abstract TValue? UpdateCurrentValue(ICollection<UploadedItem> items);

    protected virtual UploadedItem CreateUploadedItem(StorageItem storageItem)
    {
        var urls = new Dictionary<UploadedItemUrlType, string>
        {
            { UploadedItemUrlType.Full, Storage.PublicUri(storageItem).ToString() }
        };
        if (Mode == AntStorageInputMode.Image)
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

    protected void MoveBackward(UploadedItem file)
    {
        Files.MoveUp(file);
        UpdateCurrentValue();
    }

    protected void MoveForward(UploadedItem file)
    {
        Files.MoveDown(file);
        UpdateCurrentValue();
    }

    protected override bool TryParseValueFromString(string? value, out TValue result,
        out string validationErrorMessage)
    {
        result = null!;
        validationErrorMessage = "";
        return false;
    }

    public void SetValue(TValue value) => CurrentValue = value;
}

public abstract class BaseAntMultipleStorageInput<TValue> : AntStorageInput<TValue>
    where TValue : class, ICollection<StorageItem>, new()
{
    [Parameter]
    public int? MaxFiles
    {
        get => MaxAllowedFiles;
        set => MaxAllowedFiles = value;
    }

    protected override IEnumerable<UploadedItem> ParseCurrentValue(TValue currentValue) =>
        currentValue.Select(CreateUploadedItem);

    protected override TValue UpdateCurrentValue(ICollection<UploadedItem> items)
    {
        var collection = new TValue();
        foreach (var item in items)
        {
            collection.Add(item.StorageItem);
        }

        return collection;
    }
}

public abstract class BaseAntSingleStorageInput : AntStorageInput<StorageItem>
{
    protected override int? MaxAllowedFiles { get; set; } = 1;

    protected override IEnumerable<UploadedItem> ParseCurrentValue(StorageItem? currentValue)
    {
        if (currentValue is not null)
        {
            return new[] { CreateUploadedItem(currentValue) };
        }

        return Array.Empty<UploadedItem>();
    }


    protected override StorageItem? UpdateCurrentValue(ICollection<UploadedItem> items) =>
        items.FirstOrDefault()?.StorageItem;
}

public enum AntStorageInputMode
{
    File,
    Image
}
