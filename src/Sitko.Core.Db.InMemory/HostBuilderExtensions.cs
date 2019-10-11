using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.Db.InMemory
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddInMemoryDb<T>(this IHostBuilder hostBuilder, bool useContextPooling,
            string databaseName,
            IConfiguration configuration,
            Action<IServiceProvider, DbContextOptionsBuilder<T>, IConfiguration> configureInMemory) where T : DbContext
        {
            return hostBuilder.ConfigureServices((context, collection) =>
            {
                if (string.IsNullOrEmpty(databaseName)) databaseName = context.HostingEnvironment.ApplicationName;

                collection.AddInMemoryDb<T>(useContextPooling, databaseName, (serviceProvider, builder) =>
                {
                    configureInMemory(serviceProvider, builder, configuration);
                });
            });
        }
    }
}
