using Sitko.Core.App;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.FileSystem
{
    public class FileSystemStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
        FileSystemStorageMetadataProvider<TStorageOptions>, FileSystemStorageMetadataProviderOptions>
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions
    {
        public FileSystemStorageMetadataModule(Application application) : base(application)
        {
        }
    }
}
