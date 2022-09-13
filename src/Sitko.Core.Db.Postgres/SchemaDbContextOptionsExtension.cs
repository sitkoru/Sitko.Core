using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Db.Postgres;

internal class SchemaDbContextOptionsExtension : IDbContextOptionsExtension
{
    public SchemaDbContextOptionsExtension(string schema) => Schema = schema;
    public string Schema { get; }

    public bool IsCustomSchema => !string.IsNullOrEmpty(Schema) && Schema != "public";

    public void ApplyServices(IServiceCollection services) { }

    public void Validate(IDbContextOptions options) { }

    public DbContextOptionsExtensionInfo Info => new SchemaDbContextOptionsExtensionInfo(this);
}
