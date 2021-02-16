using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sitko.Core.Storage.Metadata.Postgres.DB.Models;

namespace Sitko.Core.Storage.Metadata.Postgres.DB
{
    internal class StorageDbContext : DbContext
    {
        private readonly string? _connectionString;

        public DbSet<StorageItemRecord> Records => Set<StorageItemRecord>();

        public StorageDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public StorageDbContext(DbContextOptions<StorageDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (_connectionString is not null)
            {
                optionsBuilder.UseNpgsql(_connectionString);
            }
        }
    }

    internal class StorageContextFactory : IDesignTimeDbContextFactory<StorageDbContext>
    {
        public StorageDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<StorageDbContext>();
            optionsBuilder.UseNpgsql("Data Source=blog.db");

            return new StorageDbContext(optionsBuilder.Options);
        }
    }
}
