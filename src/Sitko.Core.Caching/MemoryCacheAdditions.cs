using System.Collections.Generic;
using System.Linq;

namespace Sitko.Core.Caching
{
    public partial class MemoryCache
    {
        public IEnumerable<object> Keys()
        {
            return _entries.Keys;
        }

        public IEnumerable<object> Values()
        {
            return _entries.Values.Select(e => e.Value);
        }

        public IEnumerable<T> Keys<T>()
        {
            return _entries.Keys.Select(k => (T)k);
        }

        public IEnumerable<T> Values<T>()
        {
            return _entries.Values.Select(e => (T)e.Value);
        }

        public void Expire()
        {
            ScanForExpiredItems(this);
        }
        
    }
}
