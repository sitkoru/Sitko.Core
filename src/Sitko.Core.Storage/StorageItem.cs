using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Sitko.Core.Storage
{
    public class StorageItem : IFileInfo, IDisposable
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

                return Math.Round(size, 2) + ' ' + _units[unit];
            }
        }

        public void SetStream(Stream sourceStream)
        {
            _stream = sourceStream;
        }

        public Stream CreateReadStream()
        {
            if (_stream != null)
            {
                return _stream;
            }

            throw new Exception("No stream set for file");
        }

        public bool Exists => true;
        public long Length => FileSize;
        public string PhysicalPath => null;
        public string Name => FileName;
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDirectory => false;
        private Stream? _stream;

        public void Dispose()
        {
            _stream?.Dispose();
        }
    }
}
