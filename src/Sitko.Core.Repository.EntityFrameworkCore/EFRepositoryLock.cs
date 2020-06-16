using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryLock
    {
        private readonly EFRepositoryLockOptions _options;
        private int _locked;

        private bool Locked
        {
            get { return (Interlocked.CompareExchange(ref _locked, 1, 1) == 1); }
            set
            {
                if (value) Interlocked.CompareExchange(ref _locked, 1, 0);
                else Interlocked.CompareExchange(ref _locked, 0, 1);
            }
        }

        public EFRepositoryLock(EFRepositoryLockOptions options)
        {
            _options = options;
        }

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public async Task WaitAsync(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            await _lock.WaitAsync(timeout ?? _options.Timeout, cancellationToken);
            Locked = true;
        }

        public void Wait(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            _lock.Wait(timeout ?? _options.Timeout, cancellationToken);
            Locked = true;
        }

        public void Release()
        {
            if (Locked)
            {
                _lock.Release();
                Locked = false;
            }
        }
    }

    public class EFRepositoryLockOptions
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);
    }
}
