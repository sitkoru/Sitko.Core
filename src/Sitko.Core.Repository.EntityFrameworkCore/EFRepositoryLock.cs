using System;
using System.Threading;
using Nito.AsyncEx;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryLock
    {
        private readonly AsyncLock _mutex = new AsyncLock();

        public AwaitableDisposable<IDisposable> WaitAsync(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            return _mutex.LockAsync(cancellationToken);
        }

        public IDisposable Wait(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            return _mutex.Lock(cancellationToken);
        }
    }
}
