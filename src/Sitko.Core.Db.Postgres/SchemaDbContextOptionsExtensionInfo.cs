using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sitko.Core.Db.Postgres;

internal class SchemaDbContextOptionsExtensionInfo : DbContextOptionsExtensionInfo
{
    public SchemaDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
    {
    }
#if NET6_0_OR_GREATER
    public override int GetServiceProviderHashCode() => 0;
#else
    public override long GetServiceProviderHashCode() => 0;
#endif

#if NET6_0_OR_GREATER
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
#endif
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
    }

    public override bool IsDatabaseProvider => false;
    public override string LogFragment => nameof(SchemaDbContextOptionsExtension);
}
