using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Db.Postgres
{
    public class PostgresDatabaseModuleConfig<TDbContext> : BaseDbModuleConfig<TDbContext> where TDbContext : DbContext
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty;
        public bool EnableNpgsqlPooling { get; set; } = true;
        public bool EnableContextPooling { get; set; } = true;
        public bool EnableSensitiveLogging { get; set; } = false;

        public Assembly? MigrationsAssembly { get; set; }
        public bool AutoApplyMigrations { get; set; } = true;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Host))
                {
                    return (false, new[] {"Postgres host is empty"});
                }

                if (string.IsNullOrEmpty(Username))
                {
                    return (false, new[] {"Postgres username is empty"});
                }

                if (string.IsNullOrEmpty(Database))
                {
                    return (false, new[] {"Postgres database is empty"});
                }

                if (Port == 0)
                {
                    return (false, new[] {"Postgres port is empty"});
                }
            }

            return result;
        }
    }
}
