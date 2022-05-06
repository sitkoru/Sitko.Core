using Nito.AsyncEx;

namespace Sitko.Core.Repository.EntityFrameworkCore;

public class EFRepositoryLock
{
    public AsyncLock Lock { get; } = new();
}
