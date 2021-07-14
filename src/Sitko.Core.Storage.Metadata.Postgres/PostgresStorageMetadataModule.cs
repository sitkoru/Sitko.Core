using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.Storage.Metadata.Postgres.DB;

namespace Sitko.Core.Storage.Metadata.Postgres
{
    public class
        PostgresStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
            PostgresStorageMetadataProvider<TStorageOptions>, PostgresStorageMetadataModuleOptions<TStorageOptions>>
        where TStorageOptions : StorageOptions
    {
        public override string OptionsKey => $"Storage:Metadata:Postgres:{typeof(TStorageOptions).Name}";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            PostgresStorageMetadataModuleOptions<TStorageOptions> startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddDbContextFactory<StorageDbContext>((serviceProvider, builder) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<PostgresStorageMetadataModuleOptions<TStorageOptions>>>();
                builder.UseNpgsql(options.Value.GetConnectionString(), optionsBuilder =>
                {
                    optionsBuilder.MigrationsHistoryTable("__EFMigrationsHistory", StorageDbContext.Schema);
                });
            });
        }
    }
}
