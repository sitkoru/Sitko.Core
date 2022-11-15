using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sitko.Core.App;

namespace Sitko.Core.Db.Postgres;

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
    public int MaxMigrationsApplyTryCount { get; set; } = 1;
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;
    public Dictionary<string, object> ConnectionOptions { get; set; } = new();

    public string Schema { get; set; } = "";

    public Type GetValidatorType() => typeof(PostgresDatabaseModuleOptionsValidator<TDbContext>);

    public NpgsqlConnectionStringBuilder CreateBuilder()
    {
        var connBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
            Database = Database,
            Pooling = EnableNpgsqlPooling,
            IncludeErrorDetail = IncludeErrorDetails
        };

        foreach (var (key, value) in ConnectionOptions)
        {
            try
            {
                connBuilder[key] = value;
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    $"Can't set connection parameter {key} with value {value} for DbContext {typeof(TDbContext)}: {exception.Message}. Check options.");
            }
        }

        return connBuilder;
    }
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
