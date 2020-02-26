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

        public string? PhysicalPath { get; protected set; }

        public virtual Stream? OpenRead()
        {
            return _stream;
        }
    }
}
