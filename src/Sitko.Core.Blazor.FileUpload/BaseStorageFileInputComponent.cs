using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.FileUpload
{
    public abstract class
        BaseStorageFileInputComponent<TInput> : BaseFileInputComponent<StorageFileUploadResult, TInput>
    {
        [EditorRequired]
        [Parameter]
        public IStorage Storage { get; set; } = null!;

        [Parameter] public string UploadPath { get; set; } = "";
        [Parameter] public Func<FileUploadRequest, FileStream, Task<object>>? GenerateMetadata { get; set; }

        protected override async Task<StorageFileUploadResult> SaveFileAsync(FileUploadRequest file, FileStream stream)
        {
            object? metadata = null;
            if (GenerateMetadata is not null)
            {
                metadata = await GenerateMetadata(file, stream);
            }

            var item = await Storage.SaveAsync(stream, file.Name, UploadPath, metadata);
            return new StorageFileUploadResult(item, Storage.PublicUri(item).ToString());
        }
    }

    public abstract class BaseStorageItemInputComponent : BaseStorageFileInputComponent<StorageItem>
    {
        protected override StorageItem? GetResult(IEnumerable<StorageFileUploadResult> results) =>
            results.FirstOrDefault()?.StorageItem;
    }

    public abstract class BaseStorageItemsInputComponent : BaseStorageFileInputComponent<IEnumerable<StorageItem>>
    {
        protected override IEnumerable<StorageItem> GetResult(IEnumerable<StorageFileUploadResult> results) =>
            results.Select(r => r.StorageItem);
    }

    public class StorageFileUploadResult : IFileUploadResult
    {
        public StorageFileUploadResult(StorageItem storageItem, string url)
        {
            StorageItem = storageItem;
            Url = url;
        }

        public StorageItem StorageItem { get; }

        public string FileName => StorageItem.FileName!;
        public string FilePath => StorageItem.FilePath;
        public long FileSize => StorageItem.FileSize;
        public string Url { get; }
    }
}
