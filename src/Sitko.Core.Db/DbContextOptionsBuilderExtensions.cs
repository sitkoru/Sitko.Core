using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sitko.Core.Db;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder AddExtension<TExtension>(this DbContextOptionsBuilder optionsBuilder,
        TExtension extension)
        where TExtension : class, IDbContextOptionsExtension
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }
}
