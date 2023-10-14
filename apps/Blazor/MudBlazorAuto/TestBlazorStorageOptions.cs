using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;

namespace MudBlazorAuto;

public class TestBlazorStorageOptions : StorageOptions, IFileSystemStorageOptions
{
    public string StoragePath { get; set; } = "";
}
