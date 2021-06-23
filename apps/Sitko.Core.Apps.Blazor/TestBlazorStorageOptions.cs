using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;

namespace Sitko.Core.Apps.Blazor
{
    public class TestBlazorStorageOptions : StorageOptions, IFileSystemStorageOptions
    {
        public override string Name { get; set; }
        public string StoragePath { get; set; }
    }
}