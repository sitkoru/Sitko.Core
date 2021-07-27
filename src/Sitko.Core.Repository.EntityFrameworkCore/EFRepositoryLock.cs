namespace Sitko.Core.Repository.EntityFrameworkCore
{
    using Nito.AsyncEx;

    public class EFRepositoryLock
    {
        public AsyncLock Lock { get; } = new();
    }
}
