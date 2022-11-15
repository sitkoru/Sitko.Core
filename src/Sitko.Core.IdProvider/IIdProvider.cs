namespace Sitko.Core.IdProvider;

public interface IIdProvider
{
    Task<long> NextAsync();
}

