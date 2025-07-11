using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Queue.Kafka;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Kafka;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddKafkaTasks<TBaseTask, TDbContext>(
        this IHostApplicationBuilder applicationBuilder,
        Action<KafkaTasksModuleOptions<TBaseTask, TDbContext>> configure, bool configurePostgres = false,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configurePostgresAction = null,
        bool configureKafka = true,
        Action<KafkaModuleOptions>? configureKafkaAction = null) where TBaseTask : BaseTask
        where TDbContext : TasksDbContext<TBaseTask>
    {
        applicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>().AddKafkaTasks(configure,
            configurePostgres, configurePostgresAction, configureKafka, configureKafkaAction);
        return applicationBuilder;
    }

    public static ISitkoCoreServerApplicationBuilder AddKafkaTasks<TBaseTask, TDbContext>(this ISitkoCoreServerApplicationBuilder applicationBuilder,
        Action<KafkaTasksModuleOptions<TBaseTask, TDbContext>> configure, bool configurePostgres = false,
        Action<PostgresDatabaseModuleOptions<TDbContext>>? configurePostgresAction = null,
        bool configureKafka = true,
        Action<KafkaModuleOptions>? configureKafkaAction = null) where TBaseTask : BaseTask
        where TDbContext : TasksDbContext<TBaseTask>
    {
        applicationBuilder.AddTasks<TBaseTask, TDbContext>(configurePostgres, configurePostgresAction);
        applicationBuilder.AddModule<KafkaTasksModule<TBaseTask, TDbContext>, KafkaTasksModuleOptions<TBaseTask, TDbContext>>(
            configure);
        if (configureKafka && !applicationBuilder.HasModule<KafkaModule>())
        {
            applicationBuilder.AddModule<KafkaModule, KafkaModuleOptions>(configureKafkaAction);
        }

        return applicationBuilder;
    }
}
