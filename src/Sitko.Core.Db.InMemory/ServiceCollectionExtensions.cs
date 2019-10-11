using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Db.InMemory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryDb<T>(this IServiceCollection services, bool useContextPooling,
            string databaseName,
            Action<IServiceProvider, DbContextOptionsBuilder<T>> configureInMemory) where T : DbContext
        {
            if (useContextPooling)
            {
                services.AddDbContextPool<T>((serviceProvider, options) =>
                {
                    options.UseInMemoryDatabase(databaseName).ConfigureWarnings(
                        w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    configureInMemory?.Invoke(serviceProvider, (DbContextOptionsBuilder<T>)options);
                });
            }
            else
            {
                services.AddDbContext<T>((serviceProvider, options) =>
                {
                    options.UseInMemoryDatabase(databaseName).ConfigureWarnings(
                        w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    configureInMemory?.Invoke(serviceProvider, (DbContextOptionsBuilder<T>)options);
                });
            }

            return services;
        }
    }
}
