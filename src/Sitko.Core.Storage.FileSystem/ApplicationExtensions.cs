using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.FileSystem
{
    public static class ApplicationExtensions
    {
        public static Application AddFileSystemStorage<TStorageOptions>(this Application application,
            Action<IConfiguration, IHostEnvironment, TStorageOptions> configure, string? optionsKey = null)
            where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
        {
            return application.AddModule<FileSystemStorageModule<TStorageOptions>, TStorageOptions>(configure,
                optionsKey);
        }

        public static Application AddFileSystemStorage<TStorageOptions>(this Application application,
            Action<TStorageOptions>? configure = null, string? optionsKey = null)
            where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
        {
            return application.AddModule<FileSystemStorageModule<TStorageOptions>, TStorageOptions>(configure,
                optionsKey);
        }

        public static Application AddFileSystemStorageMetadata<TStorageOptions>(this Application application,
            Action<IConfiguration, IHostEnvironment, FileSystemStorageMetadataModuleOptions<TStorageOptions>>
                configure, string? optionsKey = null)
            where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
        {
            return application
                .AddModule<FileSystemStorageMetadataModule<TStorageOptions>,
                    FileSystemStorageMetadataModuleOptions<TStorageOptions>>(
                    configure, optionsKey);
        }

        public static Application AddFileSystemStorageMetadata<TStorageOptions>(this Application application,
            Action<FileSystemStorageMetadataModuleOptions<TStorageOptions>>?
                configure = null, string? optionsKey = null)
            where TStorageOptions : StorageOptions, IFileSystemStorageOptions, new()
        {
            return application
                .AddModule<FileSystemStorageMetadataModule<TStorageOptions>,
                    FileSystemStorageMetadataModuleOptions<TStorageOptions>>(
                    configure, optionsKey);
        }
    }
}
