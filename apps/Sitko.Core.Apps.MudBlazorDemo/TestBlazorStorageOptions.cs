using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;

namespace Sitko.Core.Apps.MudBlazorDemo
{
    public class TestBlazorStorageOptions : StorageOptions, IFileSystemStorageOptions
    {
        public string StoragePath { get; set; } = "";
    }
}
