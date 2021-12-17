using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Repository.Tests.Data;

public class SecondTestDbContext : DbContext
{
    public SecondTestDbContext(DbContextOptions<SecondTestDbContext> options) : base(options)
    {
    }

    public DbSet<FooBarModel> FooBars => Set<FooBarModel>();
}
