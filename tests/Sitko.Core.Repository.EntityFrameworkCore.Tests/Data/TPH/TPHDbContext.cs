using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data.TPH;

public class TPHDbContext : DbContext
{
    public DbSet<BaseTPHClass> Records => Set<BaseTPHClass>();
    public DbSet<FirstTPHClass> Firsts => Set<FirstTPHClass>();
    public DbSet<SecondTPHClass> Seconds => Set<SecondTPHClass>();

    public TPHDbContext(DbContextOptions<TPHDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<BaseTPHClass>()
            .HasDiscriminator(record => record.Type)
            .HasValue<FirstTPHClass>(TPHType.First)
            .HasValue<SecondTPHClass>(TPHType.Second);
    }
}
