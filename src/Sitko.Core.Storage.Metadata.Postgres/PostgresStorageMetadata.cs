namespace Sitko.Core.Storage.Metadata.Postgres
{
    public class
        PostgresStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
            PostgresStorageMetadataProvider<TStorageOptions>, PostgresStorageMetadataProviderOptions>
        where TStorageOptions : StorageOptions
    {
        public override string GetConfigKey()
        {
            return $"Storage:Metadata:Postgres:{typeof(TStorageOptions).Name}";
        }
    }
}
