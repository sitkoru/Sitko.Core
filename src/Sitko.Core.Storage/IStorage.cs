using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    public interface IStorage
    {
        Task<StorageItem> SaveFileAsync(byte[] file, string fileName, string path);
        Task<bool> DeleteFileAsync(string filePath);
    }
}
