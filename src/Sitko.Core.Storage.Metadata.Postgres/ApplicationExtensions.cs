using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Metadata.Postgres;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddPostgresStorageMetadata<TStorageOptions>(
        this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, PostgresStorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        applicationBuilder.AddSitkoCore().AddPostgresStorageMetadata(configure, optionsKey);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder AddPostgresStorageMetadata<TStorageOptions>(
        this IHostApplicationBuilder applicationBuilder,
        Action<PostgresStorageMetadataModuleOptions<TStorageOptions>>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        applicationBuilder.AddSitkoCore().AddPostgresStorageMetadata(configure, optionsKey);
        return applicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddPostgresStorageMetadata<TStorageOptions>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, PostgresStorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        applicationBuilder
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddPostgresStorageMetadata<TStorageOptions>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<PostgresStorageMetadataModuleOptions<TStorageOptions>>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        applicationBuilder
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
}
