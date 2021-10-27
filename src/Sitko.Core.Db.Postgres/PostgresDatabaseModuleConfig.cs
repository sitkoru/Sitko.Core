using System;
using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sitko.Core.App;

namespace Sitko.Core.Db.Postgres
{
    using System.Text.Json.Serialization;

    public class PostgresDatabaseModuleOptions<TDbContext> : BaseDbModuleOptions<TDbContext>,
        IModuleOptionsWithValidation where TDbContext : DbContext
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty;
        public bool EnableNpgsqlPooling { get; set; } = true;
        [JsonIgnore] public Assembly? MigrationsAssembly { get; set; }
        public bool AutoApplyMigrations { get; set; } = true;
        public Type GetValidatorType() => typeof(PostgresDatabaseModuleOptionsValidator<TDbContext>);
    }

    public class
        PostgresDatabaseModuleOptionsValidator<TDbContext> : BaseDbModuleOptionsValidator<
            PostgresDatabaseModuleOptions<TDbContext>, TDbContext> where TDbContext : DbContext
    {
        public PostgresDatabaseModuleOptionsValidator()
        {
            RuleFor(o => o.Host).NotEmpty().WithMessage("Postgres host is empty");
            RuleFor(o => o.Username).NotEmpty().WithMessage("Postgres username is empty");
            RuleFor(o => o.Database).NotEmpty().WithMessage("Postgres database is empty");
            RuleFor(o => o.Port).GreaterThan(0).WithMessage("Postgres port is empty");
        }
    }
}
