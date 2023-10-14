using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.FileSystem;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddFileSystemStorage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>().AddFileSystemStorage(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddFileSystemStorage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>().AddFileSystemStorage(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddFileSystemStorageMetadata<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, FileSystemStorageMetadataModuleOptions<TStorageOptions>>
            configure, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>().AddFileSystemStorageMetadata(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddFileSystemStorageMetadata<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<FileSystemStorageMetadataModuleOptions<TStorageOptions>>?
            configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>().AddFileSystemStorageMetadata(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddFileSystemStorage<TStorageOptions>(
        this ISitkoCoreServerApplicationBuilder applicationBuilder,
        Action<IApplicationContext, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new() =>
        applicationBuilder.AddModule<FileSystemStorageModule<TStorageOptions>, TStorageOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddFileSystemStorage<TStorageOptions>(
        this ISitkoCoreServerApplicationBuilder applicationBuilder,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new() =>
        applicationBuilder.AddModule<FileSystemStorageModule<TStorageOptions>, TStorageOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddFileSystemStorageMetadata<TStorageOptions>(
        this ISitkoCoreServerApplicationBuilder applicationBuilder,
        Action<IApplicationContext, FileSystemStorageMetadataModuleOptions<TStorageOptions>>
            configure, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new() =>
        applicationBuilder
            .AddModule<FileSystemStorageMetadataModule<TStorageOptions>,
                FileSystemStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddFileSystemStorageMetadata<TStorageOptions>(
        this ISitkoCoreServerApplicationBuilder applicationBuilder,
        Action<FileSystemStorageMetadataModuleOptions<TStorageOptions>>?
            configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new() =>
        applicationBuilder
            .AddModule<FileSystemStorageMetadataModule<TStorageOptions>,
                FileSystemStorageMetadataModuleOptions<TStorageOptions>>(
                configure, optionsKey);
}
