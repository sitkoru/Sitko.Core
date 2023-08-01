using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Db;

public interface IDbContextProvider<TDbContext> where TDbContext : DbContext
{
    TDbContext DbContext { get; }
}

internal class DbContextProvider<TDbContext> : IDbContextProvider<TDbContext> where TDbContext : DbContext
{
    public DbContextProvider(IDbContextFactory<TDbContext> dbContextFactory) =>
        DbContext = dbContextFactory.CreateDbContext();

    public TDbContext DbContext { get; }
}
