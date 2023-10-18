using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Db;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks;

public static class ApplicationExtensions
{
    public static Application AddTasks<TBaseTask, TDbContext>(this Application application, bool configurePostgres = false,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configurePostgresAction = null) where TBaseTask : BaseTask
        where TDbContext : TasksDbContext<TBaseTask>
    {
        if (configurePostgres)
        {
            application.AddPostgresDatabase<TDbContext>(options =>
            {
                configurePostgresAction?.Invoke(options);
                options.ConfigureDbContextOptions = (builder, provider, _) =>
                {
                    builder.AddExtension(provider.GetRequiredService<TasksDbContextOptionsExtension<TBaseTask>>());
                };
            });
        }

        return application;
    }
}
