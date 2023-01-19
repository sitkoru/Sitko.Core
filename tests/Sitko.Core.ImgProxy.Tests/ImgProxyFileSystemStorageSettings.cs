using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyFileSystemStorageSettings : StorageOptions, IFileSystemStorageOptions
{
    public string StoragePath { get; set; } = "/tmp/storage";
}

