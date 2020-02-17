using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Sitko.Core.Storage.FileSystem
{
    public class PhysicalDirectoryContents : IDirectoryContents
    {
        private IEnumerable<IFileInfo> _entries;
        private readonly string _directory;

        /// <summary>
        /// Initializes an instance of <see cref="PhysicalDirectoryContents"/>
        /// </summary>
        /// <param name="directory">The directory</param>
        public PhysicalDirectoryContents(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        /// <inheritdoc />
        public bool Exists => Directory.Exists(_directory);

        /// <inheritdoc />
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            EnsureInitialized();
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureInitialized();
            return _entries.GetEnumerator();
        }

        private void EnsureInitialized()
        {
            try
            {
                _entries = new DirectoryInfo(_directory)
                    .EnumerateFileSystemInfos()
                    .Select<FileSystemInfo, IFileInfo>(info =>
                    {
                        if (info is FileInfo file)
                        {
                            return new PhysicalFileInfo(file);
                        }
                        else if (info is DirectoryInfo dir)
                        {
                            return new PhysicalDirectoryInfo(dir);
                        }

                        // shouldn't happen unless BCL introduces new implementation of base type
                        throw new InvalidOperationException("Unexpected type of FileSystemInfo");
                    });
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is IOException)
            {
                _entries = Enumerable.Empty<IFileInfo>();
            }
        }
    }
}
