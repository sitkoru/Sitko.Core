namespace Sitko.Core.IdProvider;

public class FakeIdProvider : IIdProvider
{
    private long id = 100500;

    public Task<long> NextAsync()
    {
        Interlocked.Increment(ref id);
        return Task.FromResult(id);
    }
}

