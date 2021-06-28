using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Sitko.Core.App.Blazor.Components;
using Sitko.Core.App.Collections;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntStorageInput<TUploadedItem, TValue> : InputBase<TValue>, IBaseComponent
        where TUploadedItem : UploadedItem
    {
        [Parameter] public string UploadPath { get; set; } = "";
        [Parameter] public Func<FileUploadRequest, FileStream, Task<object>>? GenerateMetadata { get; set; }
        [Parameter] public Func<TValue?, Task>? OnChange { get; set; }
        [Parameter] public virtual string ContentTypes { get; set; } = "";
        [Parameter] public long MaxFileSize { get; set; }
        [Parameter] public int? MaxAllowedFiles { get; set; }
        [Parameter] public string UploadText { get; set; } = "Upload";
        [Parameter] public string PreviewText { get; set; } = "Preview";
        [Parameter] public string RemoveText { get; set; } = "Remove";
        [Parameter] public IStorage Storage { get; set; } = null!;
        [Parameter] public bool EnableOrdering { get; set; } = true;
        protected bool ShowOrdering => EnableOrdering && ItemsCount > 1;
        protected IBaseFileInputComponent? FileInput { get; set; }
        protected bool IsSpinning => FileInput?.IsLoading ?? false;

        protected bool ShowUpload => MaxAllowedFiles is null || MaxAllowedFiles < 1 || ItemsCount < MaxAllowedFiles;
        protected int? MaxFilesToUpload => MaxAllowedFiles is not null ? MaxAllowedFiles - ItemsCount : null;
        protected abstract int ItemsCount { get; }

        protected Task OnChangeAsync(TValue value)
        {
            if (OnChange is not null)
            {
                return OnChange(value);
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

        protected void RemoveFile(TUploadedItem file)
        {
            DoRemoveFile(file);
            UpdateCurrentValue();
        }

        protected void UpdateCurrentValue()
        {
            CurrentValue = GetValue();
            OnChangeAsync(CurrentValue);
        }

        protected abstract void DoRemoveFile(TUploadedItem file);
        protected abstract TUploadedItem CreateUploadedItem(StorageItem storageItem);

        protected abstract TValue GetValue();

        protected override bool TryParseValueFromString(string? value, out TValue result,
            out string validationErrorMessage)
        {
            result = default!;
            validationErrorMessage = "";
            return false;
        }

        public Task NotifyStateChangeAsync()
        {
            return InvokeAsync(StateHasChanged);
        }
    }

    public abstract class UploadedItem : IOrdered
    {
        protected UploadedItem(StorageItem storageItem)
        {
            StorageItem = storageItem;
        }

        public StorageItem StorageItem { get; }
        public abstract string Url { get; }

        public int Position { get; set; }
    }
}
