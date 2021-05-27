using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.FileSystem
{
    public class FileSystemStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
        FileSystemStorageMetadataProvider<TStorageOptions>, FileSystemStorageMetadataProviderOptions>
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions
    {
        public override string GetConfigKey()
        {
            return $"Storage:Metadata:FileSystem:{typeof(TStorageOptions).Name}";
        }
    }
}
