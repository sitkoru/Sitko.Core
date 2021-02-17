using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sitko.Core.Storage.Metadata.Postgres.DB.Models;

namespace Sitko.Core.Storage.Metadata.Postgres.DB
{
    internal class StorageDbContext : DbContext
    {
        private readonly string? _connectionString;
        private readonly string? _schema;

        public DbSet<StorageItemRecord> Records => Set<StorageItemRecord>();

        public StorageDbContext(string connectionString, string? schema = null)
        {
            _connectionString = connectionString;
            if (!string.IsNullOrEmpty(schema))
            {
                _schema = schema;
            }
        }

        public StorageDbContext(DbContextOptions<StorageDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (_connectionString is not null)
            {
                optionsBuilder.UseNpgsql(_connectionString, x =>
                {
                    if (!string.IsNullOrEmpty(_schema))
                    {
                        x.MigrationsHistoryTable("__EFMigrationsHistory", _schema);
                    }
                });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            if (!string.IsNullOrEmpty(_schema))
            {
                modelBuilder.HasDefaultSchema(_schema);
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
