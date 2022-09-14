using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sitko.Core.Db.Postgres;

internal class SchemaDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
{
    private readonly string schema;
    public SchemaDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension, string schema) : base(extension) =>
        this.schema = schema;
#if NET6_0_OR_GREATER
    public override int GetServiceProviderHashCode() => schema.GetHashCode();
#else
    public override long GetServiceProviderHashCode()  => schema.GetHashCode() ;
#endif

#if NET6_0_OR_GREATER
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
#endif
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo.Add("Schema", schema);

    public override bool IsDatabaseProvider => false;
    public override string LogFragment => $"Schema={schema}";
}
