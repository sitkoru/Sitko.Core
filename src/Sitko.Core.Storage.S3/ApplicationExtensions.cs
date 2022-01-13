using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Storage.S3;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddS3Storage<TStorageOptions>(this Application application,
        Action<IConfiguration, IAppEnvironment, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        application.AddModule<S3StorageModule<TStorageOptions>, TStorageOptions>(configure, optionsKey);

    public static Application AddS3Storage<TStorageOptions>(this Application application,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        application.AddModule<S3StorageModule<TStorageOptions>, TStorageOptions>(configure, optionsKey);

    public static Application AddS3StorageMetadata<TStorageOptions>(this Application application,
        Action<IConfiguration, IAppEnvironment, S3StorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        application
            .AddModule<S3StorageMetadataModule<TStorageOptions>, S3StorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);

    public static Application AddS3StorageMetadata<TStorageOptions>(this Application application,
        Action<S3StorageMetadataModuleOptions<TStorageOptions>>? configure = null,
        string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        application
            .AddModule<S3StorageMetadataModule<TStorageOptions>, S3StorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
}
