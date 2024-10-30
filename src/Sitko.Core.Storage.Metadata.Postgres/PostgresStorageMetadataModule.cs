namespace Sitko.Core.Storage.Metadata.Postgres;

public class
    PostgresStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
        PostgresStorageMetadataProvider<TStorageOptions>, PostgresStorageMetadataModuleOptions<TStorageOptions>>
    where TStorageOptions : StorageOptions
{
    public override string OptionsKey => $"Storage:Metadata:Postgres:{typeof(TStorageOptions).Name}";
    public override string[] OptionKeys => ["Storage:Metadata:Postgres:Default", OptionsKey];
}

