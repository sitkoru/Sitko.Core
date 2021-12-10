using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Repository.EntityFrameworkCore;

public class EFRepositoryDbContextProvider<TDbContext> where TDbContext : DbContext
{
    public EFRepositoryDbContextProvider(IDbContextFactory<TDbContext> dbContextFactory) =>
        DbContext = dbContextFactory.CreateDbContext();

    internal TDbContext DbContext { get; }
}
