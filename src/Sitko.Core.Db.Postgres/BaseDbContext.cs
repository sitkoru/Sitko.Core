using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Db.Postgres;

public abstract class BaseDbContext : DbContext
{
    private readonly DbContextOptions options;

    protected BaseDbContext(DbContextOptions options) : base(options) => this.options = options;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureSchema(options);
    }
}
