using Microsoft.EntityFrameworkCore;
using Sitko.Core.Storage.Metadata.Postgres.DB.Models;

namespace Sitko.Core.Storage.Metadata.Postgres.DB
{
    public class StorageDbContext : DbContext
    {
        public const string Schema = "storage";

        public DbSet<StorageItemRecord> Records => Set<StorageItemRecord>();

        public StorageDbContext(DbContextOptions<StorageDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(Schema);
        }
    }
}
