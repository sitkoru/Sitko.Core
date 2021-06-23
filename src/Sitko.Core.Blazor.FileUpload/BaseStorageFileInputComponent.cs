using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Storage;

namespace Sitko.Core.Blazor.FileUpload
{
    public abstract class
        BaseStorageFileInputComponent : BaseFileInputComponent<StorageFileUploadResult>
    {
        [Parameter] public IStorage Storage { get; set; } = null!;
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

    public class StorageFileUploadResult : IFileUploadResult
    {
        public StorageItem StorageItem { get; }

        public StorageFileUploadResult(StorageItem storageItem, string url)
        {
            StorageItem = storageItem;
            Url = url;
        }

        public string FileName => StorageItem.FileName!;
        public string FilePath => StorageItem.FilePath!;
        public long FileSize => StorageItem.FileSize;
        public string Url { get; }
    }
}
