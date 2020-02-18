using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Sitko.Core.Storage
{
    public class StorageItemCollection : IDirectoryContents
    {
        private readonly List<StorageItem> _files;

        public StorageItemCollection(List<StorageItem> files)
        {
            _files = files;
        }
        
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Exists => true;
    }
}
