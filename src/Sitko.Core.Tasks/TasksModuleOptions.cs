using System.Reflection;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Data.Repository;
using Sitko.Core.Tasks.Scheduling;

namespace Sitko.Core.Tasks;

public abstract class TasksModuleOptions : BaseModuleOptions, IModuleOptionsWithValidation
{
    public bool IsAllTasksDisabled { get; set; }
    public string[] DisabledTasks { get; set; } = Array.Empty<string>();
    public int? AllTasksRetentionDays { get; set; }
    public Dictionary<string, int> RetentionDays { get; set; } = new();
    public TimeSpan TasksInactivityTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan TasksWaitTimeout { get; set; } = TimeSpan.FromMinutes(60);
    public StuckTasksProcessMode StuckTasksProcessMode { get; set; } = StuckTasksProcessMode.Fail;
    public bool UseDistributedLock { get; set; } = true;
    public int AcquireTimeoutInSeconds { get; set; }
    public int RetryDelayInSeconds { get; set; } = 60;

    public abstract Type GetValidatorType();
}

public abstract class TasksModuleOptions<TBaseTask, TDbContext> : TasksModuleOptions
    where TDbContext : TasksDbContext<TBaseTask> where TBaseTask : BaseTask
{
    public List<Assembly> Assemblies { get; } = new();
    private readonly List<Action<IServiceCollection>> jobServiceConfigurations = new();

    public string TableName { get; set; } = "Tasks";

    private readonly List<Action<TasksDbContextOptionsExtension<TBaseTask>>>
        tasksDbContextOptionsExtensionConfigurations = new();

    internal void ConfigureServices(IServiceCollection serviceCollection)
    {
        foreach (var jobServiceConfiguration in jobServiceConfigurations)
        {
            jobServiceConfiguration(serviceCollection);
        }

        var extension = new TasksDbContextOptionsExtension<TBaseTask>
        {
            TableName = TableName
        };
        foreach (var configuration in tasksDbContextOptionsExtensionConfigurations)
        {
            configuration(extension);
        }

        serviceCollection.AddSingleton(extension);
    }

    public TasksModuleOptions<TBaseTask, TDbContext> AddExecutorsFromAssemblyOf<TAssembly>()
    {
        Assemblies.Add(typeof(TAssembly).Assembly);
        return this;
    }

    internal bool HasJobs => jobServiceConfigurations.Any();

    public TasksModuleOptions<TBaseTask, TDbContext> AddTask<TTask, TConfig, TResult>(string cronExpression)
        where TTask : class, IBaseTask<TConfig, TResult>
        where TConfig : BaseTaskConfig, new()
        where TResult : BaseTaskResult, new() => AddTask<TTask, TConfig, TResult>(CronExpression.Parse(cronExpression));

    public TasksModuleOptions<TBaseTask, TDbContext> AddTask<TTask, TConfig, TResult>(CronExpression cronExpression)
        where TTask : class, IBaseTask<TConfig, TResult>
        where TConfig : BaseTaskConfig, new()
        where TResult : BaseTaskResult, new()
    {
        tasksDbContextOptionsExtensionConfigurations.Add(extension =>
        {
            extension.Register<TTask, TConfig, TResult>();
        });
        jobServiceConfigurations.Add(services =>
        {
            services.Configure<TaskSchedulingOptions<TTask>>(options => options.CronExpression = cronExpression);
            var schedulerType = typeof(IBaseTaskFactory<TTask>);
            services.Scan(selector =>
                selector.FromAssemblyOf<TTask>()
                    .AddClasses(filter => filter.AssignableToAny(schedulerType))
                    .As<IBaseTaskFactory<TTask>>().WithScopedLifetime());
            services.AddSingleton(typeof(IHostedService),
                typeof(TaskSchedulingService<,>).MakeGenericType(typeof(TTask), GetType()));
            services.Scan(selector => selector.FromTypes(typeof(BaseTaskRepository<TTask, TBaseTask, TDbContext>))
                .AsSelf().As<IRepository>().As<IRepository<TTask, Guid>>().As<ITaskRepository<TTask>>()
                .WithTransientLifetime());
        });
        return this;
    }
}

public enum StuckTasksProcessMode
{
    Fail = 1,
    Restart = 2
}
