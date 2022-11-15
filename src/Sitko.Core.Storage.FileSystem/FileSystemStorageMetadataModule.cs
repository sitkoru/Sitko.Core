using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.FileSystem;

public class FileSystemStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
    FileSystemStorageMetadataProvider<TStorageOptions>, FileSystemStorageMetadataModuleOptions<TStorageOptions>>
    where TStorageOptions : StorageOptions, IFileSystemStorageOptions
{
    public override string OptionsKey => $"Storage:Metadata:FileSystem:{typeof(TStorageOptions).Name}";
}

