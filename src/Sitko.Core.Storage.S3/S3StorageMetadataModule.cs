using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.S3;

public class S3StorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
    S3StorageMetadataProvider<TStorageOptions>, S3StorageMetadataModuleOptions<TStorageOptions>>
    where TStorageOptions : S3StorageOptions, new()
{
    public override string OptionsKey => $"Storage:Metadata:S3:{typeof(TStorageOptions).Name}";
}

