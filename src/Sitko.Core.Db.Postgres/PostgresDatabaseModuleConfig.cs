using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Db.Postgres
{
    public class PostgresDatabaseModuleConfig<TDbContext> : BaseDbModuleConfig<TDbContext> where TDbContext : DbContext
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool EnableNpgsqlPooling { get; set; } = true;
        public bool EnableContextPooling { get; set; } = true;
        public bool EnableSensitiveLogging { get; set; }

        public Assembly? MigrationsAssembly
        {
            get;
        }

        public PostgresDatabaseModuleConfig(string host, string username, string database,
            string password = "",
            int port = 5432, Assembly? migrationsAssembly = null) : base(database)
        {
            Host = host;
            Username = username;
            MigrationsAssembly = migrationsAssembly;
            Password = password;
            Port = port;
        }
    }
}
