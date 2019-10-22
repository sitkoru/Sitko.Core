using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Sitko.Core.App.Collections
{
    public class ConcurrentDictionaryWithTtl<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, IDisposable
    {
        private readonly Func<TValue, DateTimeOffset> _expirationPredicate;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _frequency = TimeSpan.FromSeconds(30);
        private readonly Timer _timer;

        public ConcurrentDictionaryWithTtl(Func<TValue, DateTimeOffset> expirationPredicate, TimeSpan? ttl = null,
            TimeSpan? frequency = null)
        {
            _expirationPredicate = expirationPredicate;
            if (ttl.HasValue) _ttl = ttl.Value;
            if (frequency.HasValue) _frequency = frequency.Value;
            _timer = new Timer(state => Expire(), null, TimeSpan.Zero, _frequency);
        }

        private void Expire()
        {
            var expireDate = DateTimeOffset.UtcNow - _ttl;
            var expired = this.Where(item => _expirationPredicate(item.Value) < expireDate).ToList();
            foreach (var item in expired)
            {
                TryRemove(item.Key, out _);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
