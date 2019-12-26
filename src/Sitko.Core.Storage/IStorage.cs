using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    // ReSharper disable once UnusedTypeParameter
    public interface IStorage<T> where T : IStorageOptions
    {
        Task<StorageItem> SaveFileAsync(Stream file, string fileName, string path);

        Task<StorageItem> SaveImageAsync(Stream file, string fileName, string path,
            List<StorageImageSize>? sizes = null);

        Task<bool> DeleteFileAsync(string filePath);
    }
}
