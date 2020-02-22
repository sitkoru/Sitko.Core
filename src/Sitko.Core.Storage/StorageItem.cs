using System;

namespace Sitko.Core.Storage
{
    public class StorageItem
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
        public string Path { get; set; }
        public string StorageFileName => FilePath.Substring(FilePath.LastIndexOf('/') + 1);
        
        public string HumanSize
        {
            get
            {
                return Helpers.HumanSize(FileSize);
            }
        }

        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    }
}
