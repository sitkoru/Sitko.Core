using System;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    public interface IStorage
    {
        Task<StorageItem> SaveFileAsync(Stream file, string fileName, string path);
        Task<StorageRecord?> GetFileAsync(string path);
        Task<bool> DeleteFileAsync(string filePath);
        Task<bool> IsFileExistsAsync(string path);
        Task DeleteAllAsync();
        Task<StorageItemCollection> GetDirectoryContentsAsync(string path);
        Uri PublicUri(StorageItem item);
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IStorage<T> : IStorage where T : StorageOptions
    {
    }
}
