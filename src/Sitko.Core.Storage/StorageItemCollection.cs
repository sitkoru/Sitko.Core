using System.Collections;
using System.Collections.Generic;

namespace Sitko.Core.Storage
{
    public class StorageItemCollection : IEnumerable<StorageItem>
    {
        private readonly List<StorageItem> _files;

        public StorageItemCollection(List<StorageItem> files)
        {
            _files = files;
        }

        public IEnumerator<StorageItem> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
