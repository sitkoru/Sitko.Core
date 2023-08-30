using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Db;

public interface IDbContextProvider<TDbContext> where TDbContext : DbContext
{
    TDbContext DbContext { get; }
    TDbContext CreateDbContext();
}

internal class DbContextProvider<TDbContext> : IDbContextProvider<TDbContext> where TDbContext : DbContext
{
    private readonly IDbContextFactory<TDbContext> dbContextFactory;

    public DbContextProvider(IDbContextFactory<TDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
        DbContext = CreateDbContext();
    }

    public TDbContext CreateDbContext() => dbContextFactory.CreateDbContext();

    public TDbContext DbContext { get; }
}
