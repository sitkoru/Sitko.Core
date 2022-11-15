using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sitko.Core.Db.Postgres;

internal class SchemaDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
{
    private readonly string schema;

    public SchemaDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension, string schema) : base(extension) =>
        this.schema = schema;

    public override int GetServiceProviderHashCode() => schema.GetHashCode();

    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo.Add("Schema", schema);

    public override bool IsDatabaseProvider => false;
    public override string LogFragment => $"Schema={schema}";
}
