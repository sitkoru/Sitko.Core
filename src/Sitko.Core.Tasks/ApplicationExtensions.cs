using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Db;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddTasks<TBaseTask, TDbContext>(
        this IHostApplicationBuilder applicationBuilder, bool configurePostgres = false,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configurePostgresAction = null) where TBaseTask : BaseTask
        where TDbContext : TasksDbContext<TBaseTask>
    {
        applicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>()
            .AddTasks<TBaseTask, TDbContext>(configurePostgres, configurePostgresAction);
        return applicationBuilder;
    }

    public static ISitkoCoreServerApplicationBuilder AddTasks<TBaseTask, TDbContext>(
        this ISitkoCoreServerApplicationBuilder applicationBuilder, bool configurePostgres = false,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configurePostgresAction = null) where TBaseTask : BaseTask
        where TDbContext : TasksDbContext<TBaseTask>
    {
        if (configurePostgres)
        {
            applicationBuilder.AddPostgresDatabase<TDbContext>(options =>
            {
                configurePostgresAction?.Invoke(options);
                options.ConfigureDbContextOptions = (builder, provider, _) =>
                {
                    builder.AddExtension(provider.GetRequiredService<TasksDbContextOptionsExtension<TBaseTask>>());
                };
            });
        }

        return applicationBuilder;
    }
}
