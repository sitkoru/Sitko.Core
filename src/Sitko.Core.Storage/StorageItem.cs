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
        private readonly string[] _units = {"bytes", "KB", "MB", "GB", "TB", "PB"};

        public string HumanSize
        {
            get
            {
                if (FileSize < 1)
                {
                    return "-";
                }

                var unit = 0;

                double size = FileSize;
                while (size >= 1024)
                {
                    size /= 1024;
                    unit++;
                }

                return $"{Math.Round(size, 2):N}{_units[unit]}";
            }
        }

        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    }
}
