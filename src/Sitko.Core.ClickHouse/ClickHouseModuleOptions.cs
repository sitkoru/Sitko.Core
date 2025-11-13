using ClickHouse.Driver.ADO;
using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.ClickHouse;

public class ClickHouseModuleOptions : BaseModuleOptions,
    IModuleOptionsWithValidation
{
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 8123;
    public string UserName { get; set; } = "clickhouse";
    public string Password { get; set; } = "";
    public string Database { get; set; } = "";
    public bool UseSession { get; set; }
    public bool WithSsl { get; set; }
    public Dictionary<string, string> Settings { get; set; } = [];

    public ClickHouseConnectionStringBuilder GetConnection(Dictionary<string, string>? settings = null, string? dbName = null)
    {
        var builder = new ClickHouseConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = dbName ?? Database,
            Username = UserName,
            Password = Password,
            UseSession = UseSession
        };
        foreach (var (key, value) in Settings)
        {
            builder.Add(key, value);
        }

        if (settings != null)
        {
            foreach (var (key, value) in settings)
            {
                builder.Add(key, value);
            }
        }

        if (WithSsl)
        {
            builder.Protocol = "https";
        }

        return builder;
    }

    public Type GetValidatorType() => typeof(ClickHouseModuleOptionsValidator);

    public string GetConnectionString(Dictionary<string, string>? settings = null, string? dbName = null) =>
        GetConnection(settings, dbName).ConnectionString;
}

public class
    ClickHouseModuleOptionsValidator : AbstractValidator<ClickHouseModuleOptions>
{
    public ClickHouseModuleOptionsValidator()
    {
        RuleFor(o => o.Host).NotEmpty().WithMessage("ClickHouse host is empty");
        RuleFor(o => o.UserName).NotEmpty().WithMessage("ClickHouse username is empty");
        RuleFor(o => o.Database).NotEmpty().WithMessage("ClickHouse database is empty");
        RuleFor(o => o.Port).GreaterThan((ushort)0).WithMessage("ClickHouse port is empty");
    }
}
