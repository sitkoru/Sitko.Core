using System.Threading;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public class EFRepositoryLock
    {
        public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);
    }
}
