using System;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    // ReSharper disable once UnusedTypeParameter
    public interface IStorage<T> where T : StorageOptions
    {
        Task<StorageItem> SaveFileAsync(Stream file, string fileName, string path);
        Task<StorageItem?> GetFileAsync(string path);
        Task<bool> DeleteFileAsync(string filePath);

        Task<bool> IsFileExistsAsync(string path);
        Task<Stream?> DownloadFileAsync(string path);

        Task DeleteAllAsync();


        Task<StorageItemCollection> GetDirectoryContentsAsync(string path);

        Uri PublicUri(StorageItem item);
    }
}
