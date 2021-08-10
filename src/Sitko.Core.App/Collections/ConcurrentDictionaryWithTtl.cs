using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Sitko.Core.App.Collections
{
    public class ConcurrentDictionaryWithTtl<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, IDisposable where TKey : notnull
    {
        private readonly Func<TValue, DateTimeOffset> expirationPredicate;
        private readonly TimeSpan ttl = TimeSpan.FromMinutes(30);
        private readonly TimeSpan frequency = TimeSpan.FromSeconds(30);
        private readonly Timer? timer;

        public ConcurrentDictionaryWithTtl(Func<TValue, DateTimeOffset> expirationPredicate, TimeSpan? ttl = null,
            TimeSpan? frequency = null)
        {
            this.expirationPredicate = expirationPredicate;
            if (ttl.HasValue)
            {
                this.ttl = ttl.Value;
            }

            if (frequency.HasValue)
            {
                this.frequency = frequency.Value;
            }

            timer = new Timer(_ => Expire(), null, TimeSpan.Zero, this.frequency);
        }

        private void Expire()
        {
            var expireDate = DateTimeOffset.UtcNow - ttl;
            var expired = this.Where(item => expirationPredicate(item.Value) < expireDate).ToList();
            foreach (var item in expired)
            {
                TryRemove(item.Key, out _);
            }
        }

        public void Dispose()
        {
            timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
