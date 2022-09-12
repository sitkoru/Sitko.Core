using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Db.Postgres;

public abstract class BaseDbContext : DbContext
{
    private readonly IOptions<IPostgresDatabaseModuleOptions> postgresOptions;

    protected BaseDbContext(IOptions<IPostgresDatabaseModuleOptions> postgresOptions,
        DbContextOptions options) :
        base(options) =>
        this.postgresOptions = postgresOptions;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (!string.IsNullOrEmpty(postgresOptions.Value.Schema) && postgresOptions.Value.Schema != "public")
        {
            modelBuilder.HasDefaultSchema(postgresOptions.Value.Schema);
        }
    }
}
