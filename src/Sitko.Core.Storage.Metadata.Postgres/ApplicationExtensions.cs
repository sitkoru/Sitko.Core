using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Metadata.Postgres
{
    public static class ApplicationExtensions
    {
        public static Application AddPostgresStorageMetadata<TStorageOptions>(this Application application,
            Action<IConfiguration, IHostEnvironment, PostgresStorageMetadataModuleOptions<TStorageOptions>> configure,
            string? optionsKey = null)
            where TStorageOptions : StorageOptions
        {
            return application
                .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                    PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                    configure, optionsKey);
        }

        public static Application AddPostgresStorageMetadata<TStorageOptions>(this Application application,
            Action<PostgresStorageMetadataModuleOptions<TStorageOptions>>? configure = null, string? optionsKey = null)
            where TStorageOptions : StorageOptions
        {
            return application
                .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                    PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                    configure, optionsKey);
        }
    }
}
