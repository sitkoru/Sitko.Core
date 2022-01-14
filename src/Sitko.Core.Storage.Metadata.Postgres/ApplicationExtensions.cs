using System;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Metadata.Postgres;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddPostgresStorageMetadata<TStorageOptions>(this Application application,
        Action<IApplicationContext, PostgresStorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        application
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);

    public static Application AddPostgresStorageMetadata<TStorageOptions>(this Application application,
        Action<PostgresStorageMetadataModuleOptions<TStorageOptions>>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        application
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
}
