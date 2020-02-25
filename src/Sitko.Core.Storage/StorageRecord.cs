using System;
using System.IO;

namespace Sitko.Core.Storage
{
    public class StorageRecord : StorageItem
    {
        private readonly Stream? _stream;

        public StorageRecord()
        {
        }
        
        public StorageRecord(StorageItem item) : base(item)
        {
        }

        public StorageRecord(StorageItem item, Stream stream) : base(item)
        {
            _stream = stream;
        }

        public DateTimeOffset LastModified { get; set; }

        public virtual string? PhysicalPath { get; }

        public virtual Stream? OpenRead()
        {
            return _stream;
        }
    }
}
