using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sitko.Core.Storage.Metadata.Postgres.DB.Models
{
    [Table("StorageItemRecords")]
    public class StorageItemRecord
    {
        public StorageItemRecord()
        {
        }

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
        public string Storage { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;
        public string Path { get; set; }

        [Column(TypeName = "jsonb")] public StorageItemMetadata? Metadata { get; set; }
    }
}
