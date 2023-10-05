using Sitko.Core.App;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Kafka;

public static class ApplicationExtensions
{
    public static Application AddKafkaTasks<TBaseTask, TDbContext>(this Application application,
        Action<KafkaTasksModuleOptions<TBaseTask, TDbContext>> configure, bool configurePostgres = false,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configurePostgresAction = null) where TBaseTask : BaseTask
        where TDbContext : TasksDbContext<TBaseTask>
    {
        application.AddTasks<TBaseTask, TDbContext>(configurePostgres, configurePostgresAction);
        application.AddModule<KafkaTasksModule<TBaseTask, TDbContext>, KafkaTasksModuleOptions<TBaseTask, TDbContext>>(
            configure);

        return application;
    }
}
