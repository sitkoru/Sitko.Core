using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Storage.Metadata.Postgres.DB;

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
        applicationBuilder.GetSitkoCore().AddPostgresStorageMetadata(configure, optionsKey);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder AddPostgresStorageMetadata<TStorageOptions>(
        this IHostApplicationBuilder applicationBuilder,
        Action<PostgresStorageMetadataModuleOptions<TStorageOptions>>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        applicationBuilder.GetSitkoCore().AddPostgresStorageMetadata(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddPostgresStorageMetadata<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, PostgresStorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        AddPostgresDatabase<TStorageOptions>(applicationBuilder);
        return applicationBuilder
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
    }

    public static ISitkoCoreApplicationBuilder AddPostgresStorageMetadata<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<PostgresStorageMetadataModuleOptions<TStorageOptions>>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        AddPostgresDatabase<TStorageOptions>(applicationBuilder);
        return applicationBuilder
            .AddModule<PostgresStorageMetadataModule<TStorageOptions>,
                PostgresStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
    }

    private static void AddPostgresDatabase<TStorageOptions>(ISitkoCoreApplicationBuilder applicationBuilder)
        where TStorageOptions : StorageOptions =>
        applicationBuilder.AddPostgresDatabase<StorageDbContext>(options =>
            {
                options.EnableJsonConversion = true;
                options.Schema = StorageDbContext.Schema;
            }, $"Storage:Metadata:Postgres:{typeof(TStorageOptions).Name}");
}
