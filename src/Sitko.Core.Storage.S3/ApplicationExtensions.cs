﻿using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.S3;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddS3Storage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore().AddS3Storage(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddS3Storage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore().AddS3Storage(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddS3StorageMetadata<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, S3StorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore().AddS3StorageMetadata(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddS3StorageMetadata<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<S3StorageMetadataModuleOptions<TStorageOptions>>? configure = null,
        string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore().AddS3StorageMetadata(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddS3Storage<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        applicationBuilder.AddModule<S3StorageModule<TStorageOptions>, TStorageOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddS3Storage<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        applicationBuilder.AddModule<S3StorageModule<TStorageOptions>, TStorageOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddS3StorageMetadata<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, S3StorageMetadataModuleOptions<TStorageOptions>> configure,
        string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        applicationBuilder
            .AddModule<S3StorageMetadataModule<TStorageOptions>, S3StorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddS3StorageMetadata<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<S3StorageMetadataModuleOptions<TStorageOptions>>? configure = null,
        string? optionsKey = null)
        where TStorageOptions : S3StorageOptions, new() =>
        applicationBuilder
            .AddModule<S3StorageMetadataModule<TStorageOptions>, S3StorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
}
