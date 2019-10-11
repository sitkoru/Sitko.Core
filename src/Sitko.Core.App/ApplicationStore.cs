using System.Collections.Generic;

namespace Sitko.Core.App
{
    public class ApplicationStore
    {
        private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

        public void Set(string key, object value)
        {
            _store[key] = value;
        }

        public T Get<T>(string key)
        {
            if (_store.ContainsKey(key))
            {
                return (T)_store[key];
            }

            return default;
        }
    }
}
