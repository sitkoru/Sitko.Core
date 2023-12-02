using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Sitko.Core.App;
using Sitko.Core.Storage.Metadata.Postgres.DB;

namespace Sitko.Core.Storage.Metadata.Postgres;

public class
    PostgresStorageMetadataModule<TStorageOptions> : BaseStorageMetadataModule<TStorageOptions,
        PostgresStorageMetadataProvider<TStorageOptions>, PostgresStorageMetadataModuleOptions<TStorageOptions>>
    where TStorageOptions : StorageOptions
{
    public override string OptionsKey => $"Storage:Metadata:Postgres:{typeof(TStorageOptions).Name}";
    public override string[] OptionKeys => new[] { "Storage:Metadata:Postgres:Default", OptionsKey };

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        PostgresStorageMetadataModuleOptions<TStorageOptions> startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddDbContextFactory<StorageDbContext>((serviceProvider, builder) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<PostgresStorageMetadataModuleOptions<TStorageOptions>>>();
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(options.Value.GetConnectionString());
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();
            builder.UseNpgsql(dataSource, optionsBuilder =>
            {
                optionsBuilder.MigrationsHistoryTable("__EFMigrationsHistory", StorageDbContext.Schema);
            });
        });
    }
}
