using Microsoft.EntityFrameworkCore.Infrastructure;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Data;

internal class TasksDbContextOptionsExtensionInfo<TBaseTask> : DbContextOptionsExtensionInfo where TBaseTask : BaseTask
{
    public TasksDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
    {
    }

#if NET6_0_OR_GREATER
    public override int GetServiceProviderHashCode() => Extension.GetHashCode();
#else
    public override long GetServiceProviderHashCode() => Extension.GetHashCode();
#endif

#if NET6_0_OR_GREATER
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
#endif
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
    }

    public override bool IsDatabaseProvider => false;
    public override string LogFragment => nameof(TasksDbContextOptionsExtension<TBaseTask>);
}
