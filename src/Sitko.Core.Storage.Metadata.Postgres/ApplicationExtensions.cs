using JetBrains.Annotations;
using Sitko.Core.App;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Storage.Metadata.Postgres.DB;

namespace Sitko.Core.Storage.Metadata.Postgres;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddPostgresStorageMetadata<TStorageOptions>(this Application application,
        Action<IApplicationContext, PostgresStorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        AddPostgresDatabase<TStorageOptions>(application);
        return application
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
    }

    public static Application AddPostgresStorageMetadata<TStorageOptions>(this Application application,
        Action<PostgresStorageMetadataModuleOptions<TStorageOptions>>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        AddPostgresDatabase<TStorageOptions>(application);
        return application
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
    }

    private static void AddPostgresDatabase<TStorageOptions>(Application application)
        where TStorageOptions : StorageOptions =>
        application.AddPostgresDatabase<StorageDbContext>(options =>
            {
                options.Schema = StorageDbContext.Schema;
            }, $"Storage:Metadata:Postgres:{typeof(TStorageOptions).Name}");
}

