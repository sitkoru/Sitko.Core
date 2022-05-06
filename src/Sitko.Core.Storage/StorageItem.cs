using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MimeMapping;
using Sitko.Core.App.Helpers;
using Sitko.Core.Storage.Internal;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage;

public sealed record StorageItem
{
    [JsonConstructor]
    public StorageItem()
    {
    }

    public StorageItem(string path, StorageItemMetadata? metadata = null)
    {
        FilePath = path;
        Path = Helpers.PreparePath(System.IO.Path.GetDirectoryName(path))!;
        FileName = metadata?.FileName ?? System.IO.Path.GetFileName(path);
        MetadataJson = metadata?.Data;
        MimeType = MimeUtility.GetMimeMapping(path);
    }

    /// <summary>
    ///     Name of uploaded file
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    ///     Size of uploaded file
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    ///     MimeType of uploaded file
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    ///     Full path to uploaded file in storage
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    ///     Last modified date of uploaded file
    /// </summary>
    public DateTimeOffset LastModified { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Path without file name to uploaded file in storage
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    ///     Uploaded file metadata JSON. Read-only.
    /// </summary>
    public string? MetadataJson { get; init; }

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
