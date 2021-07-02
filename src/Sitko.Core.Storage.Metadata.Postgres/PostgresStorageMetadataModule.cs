using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Sitko.Core.App;
using Sitko.Core.Storage.Metadata.Postgres.DB;

namespace Sitko.Core.Storage.Metadata.Postgres
{
    public class
        PostgresStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
            PostgresStorageMetadataProvider<TStorageOptions>, PostgresStorageMetadataProviderOptions>
        where TStorageOptions : StorageOptions
    {
        public override string GetOptionsKey()
        {
            return $"Storage:Metadata:Postgres:{typeof(TStorageOptions).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            PostgresStorageMetadataProviderOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddDbContextFactory<StorageDbContext>((serviceProvider, builder) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<PostgresStorageMetadataProviderOptions>>();
                var connBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = options.Value.Host,
                    Port = options.Value.Port,
                    Username = options.Value.Username,
                    Password = options.Value.Password,
                    Database = options.Value.Database
                };
                builder.UseNpgsql(connBuilder.ConnectionString, optionsBuilder =>
                {
                    optionsBuilder.MigrationsHistoryTable("__EFMigrationsHistory", StorageDbContext.Schema);
                });
            });
        }
    }
}
