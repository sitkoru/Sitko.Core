using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Db.Postgres
{
    public class PostgresDatabaseModuleConfig<TDbContext> : BaseDbModuleConfig<TDbContext> where TDbContext : DbContext
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableNpgsqlPooling { get; set; } = true;
        public bool EnableContextPooling { get; set; } = true;
        public bool EnableSensitiveLogging { get; set; } = false;

        public Assembly? MigrationsAssembly { get; set; }
    }
}
