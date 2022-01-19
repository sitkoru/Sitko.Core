using System;
using System.Text.Json;
using MimeMapping;
using Sitko.Core.App.Helpers;
using Sitko.Core.Storage.Internal;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage;

public sealed record StorageItem
{
    public StorageItem()
    {
    }

    public StorageItem(string destinationPath,
        DateTimeOffset date,
        long fileSize, string? prefix, StorageItemMetadata? metadata = null)
    {
        destinationPath = Helpers.GetPathWithoutPrefix(prefix, destinationPath);
        var fileName = metadata?.FileName ?? System.IO.Path.GetFileName(destinationPath);
        Path = Helpers.PreparePath(System.IO.Path.GetDirectoryName(destinationPath))!;
        FileName = fileName;
        LastModified = date;
        FileSize = fileSize;
        FilePath = destinationPath;
        MetadataJson = metadata?.Data;
        MimeType = MimeUtility.GetMimeMapping(fileName);
    }

    internal StorageItem(string path, StorageItemDownloadInfo storageItemInfo, string? prefix) : this(path,
        storageItemInfo.Date,
        storageItemInfo.FileSize, prefix, storageItemInfo.Metadata)
    {
    }

    internal StorageItem(StorageItemInfo storageItemInfo, string? prefix, StorageItemMetadata? metadata = null) :
        this(storageItemInfo.Path,
            storageItemInfo.Date,
            storageItemInfo.FileSize, prefix, metadata)
    {
    }

    /// <summary>
    ///     Name of uploaded file
    /// </summary>
    public string? FileName { get; set; } = string.Empty;

    /// <summary>
    ///     Size of uploaded file
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    ///     MimeType of uploaded file
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    ///     Full path to uploaded file in storage
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Last modified date of uploaded file
    /// </summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;

    /// <summary>
    ///     Path without file name to uploaded file in storage
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    ///     Uploaded file metadata JSON. Read-only.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    ///     Uploaded file size in human-readable format
    /// </summary>
    public string HumanSize => FilesHelper.HumanSize(FileSize);

    /// <summary>
    ///     Get uploaded file metadata mapped to object
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    /// <returns>Uploaded file metadata object (TMedatata)</returns>
    public TMetadata? GetMetadata<TMetadata>() where TMetadata : class => string.IsNullOrEmpty(MetadataJson)
        ? null
        : JsonSerializer.Deserialize<TMetadata>(MetadataJson);
}
