using Sitko.Core.App;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.S3
{
    public class S3StorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
        S3StorageMetadataProvider<TStorageOptions>, S3StorageMetadataProviderOptions>
        where TStorageOptions : S3StorageOptions, new()
    {
        public override string GetConfigKey()
        {
            return $"Storage:Metadata:FileSystem:{typeof(TStorageOptions).Name}";
        }

        public S3StorageMetadataModule(Application application) : base(application)
        {
        }
    }
}
