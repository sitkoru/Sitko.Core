using System;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    // ReSharper disable once UnusedTypeParameter
    public interface IStorage<T> where T : StorageOptions
    {
        Task<StorageItem> SaveFileAsync(Stream file, string fileName, string path);
        Task<bool> DeleteFileAsync(string filePath);

        Task<Stream> DownloadFileAsync(StorageItem item);
        Task<bool> IsFileExistsAsync(StorageItem item);

        Task DeleteAllAsync();

        Task<StorageItem> GetFileInfoAsync(StorageItem item);
        Task<StorageItemCollection> GetDirectoryContentsAsync(string path);

        Uri PublicUri(StorageItem item);
    }
}
