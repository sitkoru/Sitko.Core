using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace Sitko.Core.Storage.Metadata.Postgres.DB.Models;

[PublicAPI]
[Table(StorageDbContext.Table, Schema = StorageDbContext.Schema)]
public class StorageItemRecord
{
    // Used for EF
    // ReSharper disable once UnusedMember.Global
    public StorageItemRecord() => LastModified = DateTimeOffset.UtcNow;

    public StorageItemRecord(string storage, StorageItem storageItem)
    {
        Storage = storage;
        FilePath = storageItem.FilePath;
        FileSize = storageItem.FileSize;
        FileName = storageItem.FileName!;
        MimeType = storageItem.MimeType;
        LastModified = storageItem.LastModified;
        Path = storageItem.Path;
    }

    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Storage { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public DateTimeOffset LastModified { get; set; }
    public string Path { get; set; } = "";

    [Column(TypeName = "jsonb")] public StorageItemMetadata? Metadata { get; set; }

    public StorageItem StorageItem => new(FilePath, Metadata)
    {
        FileSize = FileSize,
        LastModified = LastModified,
        MimeType = MimeType,
        Path = Path,
        FileName = Metadata?.FileName ?? FileName
    };
}
