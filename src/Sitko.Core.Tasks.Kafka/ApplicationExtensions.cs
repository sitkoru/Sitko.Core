using Sitko.Core.App;

namespace Sitko.Core.Tasks.Kafka;

public static class ApplicationExtensions
{
    public static Application AddKafkaTasks<TBaseTask, TDbContext>(this Application application,
        Action<KafkaTasksModuleOptions<TBaseTask, TDbContext>> configure) where TBaseTask : BaseTask
        where TDbContext : TasksDbContext<TBaseTask>
    {
        application.AddModule<KafkaTasksModule<TBaseTask, TDbContext>, KafkaTasksModuleOptions<TBaseTask, TDbContext>>(
            configure);
        return application;
    }
}
