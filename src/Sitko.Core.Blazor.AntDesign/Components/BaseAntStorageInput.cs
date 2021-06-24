using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Blazor.Components;
using Sitko.Core.App.Collections;
using Sitko.Core.Blazor.FileUpload;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntStorageInput<TUploadedItem> : BaseComponent where TUploadedItem : UploadedItem
    {
        [Parameter] public string UploadPath { get; set; } = "";
        [Parameter] public Func<FileUploadRequest, FileStream, Task<object>>? GenerateMetadata { get; set; }
        [Parameter] public virtual string ContentTypes { get; set; } = "";
        [Parameter] public long MaxFileSize { get; set; }
        [Parameter] public int? MaxAllowedFiles { get; set; }
        [Parameter] public string UploadText { get; set; } = "Upload";
        [Parameter] public string PreviewText { get; set; } = "Preview";
        [Parameter] public string RemoveText { get; set; } = "Remove";
        [Parameter] public IStorage Storage { get; set; } = null!;
        [Parameter] public bool EnableOrdering { get; set; } = true;
        protected bool ShowOrdering => EnableOrdering && ItemsCount > 1;
        protected AntStorageFileInput? FileInput { get; set; }
        protected bool IsSpinning => FileInput?.IsLoading ?? false;

        protected bool ShowUpload => MaxAllowedFiles is null || MaxAllowedFiles < 1 || ItemsCount < MaxAllowedFiles;
        protected int? MaxFilesToUpload => MaxAllowedFiles is not null ? MaxAllowedFiles - ItemsCount : null;
        protected abstract int ItemsCount { get; }

        protected Task<object> GenerateMetadataAsync(FileUploadRequest request, FileStream stream)
        {
            if (GenerateMetadata is not null)
            {
                return GenerateMetadata(request, stream);
            }

            return Task.FromResult((object)null!);
        }

        protected Task FilesUploadedAsync(IEnumerable<StorageFileUploadResult> results)
        {
            AddFiles(results.Select(r => CreateUploadedItem(r.StorageItem)));
            return UpdateFilesAsync();
        }

        protected abstract void AddFiles(IEnumerable<TUploadedItem> items);

        protected abstract TUploadedItem CreateUploadedItem(StorageItem storageItem);

        protected Task RemoveFileAsync(TUploadedItem file)
        {
            RemoveFile(file);
            return UpdateFilesAsync();
        }

        protected abstract void RemoveFile(TUploadedItem file);

        protected async Task UpdateFilesAsync()
        {
            await UpdateStorageItems();
            await NotifyStateChangeAsync();
        }

        protected abstract Task UpdateStorageItems();
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
