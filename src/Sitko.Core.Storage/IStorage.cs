using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    public interface IStorage
    {
        Task<StorageItem> SaveAsync(Stream file, string fileName, string path,
            object? metadata = null);

        Task<StorageItem?> GetAsync(string path);
        Task<DownloadResult?> DownloadAsync(string path);
        Task<bool> DeleteAsync(string filePath);
        Task<bool> IsExistsAsync(string path);
        Task DeleteAllAsync();
        Task<IEnumerable<StorageNode>> GetDirectoryContentsAsync(string path);
        Uri PublicUri(StorageItem item);
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IStorage<T> : IStorage where T : StorageOptions
    {
    }
}
