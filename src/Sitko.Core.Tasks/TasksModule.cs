using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.BackgroundServices;
using Sitko.Core.Tasks.Components;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Data.Repository;
using Sitko.Core.Tasks.Execution;
using Sitko.Core.Tasks.Scheduling;

namespace Sitko.Core.Tasks;

public abstract class
    TasksModule<TBaseTask, TDbContext, TTaskScheduler, TOptions> : BaseApplicationModule<TOptions>
    where TOptions : TasksModuleOptions<TBaseTask, TDbContext>, new()
    where TDbContext : TasksDbContext<TBaseTask>
    where TTaskScheduler : class, ITaskScheduler
    where TBaseTask : BaseTask
{
    public override string OptionsKey => $"Tasks:{typeof(TBaseTask).Name}";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);

        var types = new List<Type>();
        if (startupOptions.Assemblies.Count > 0)
        {
            foreach (var assembly in startupOptions.Assemblies)
            {
                types.AddRange(assembly.ExportedTypes.Where(type => typeof(ITaskExecutor).IsAssignableFrom(type)));
            }
        }

        var executors = new List<ExecutorRegistration>();
        foreach (var executorType in types)
        {
            var groupInfo = TaskExecutorHelper.GetGroupInfo(executorType);
            if (groupInfo is null)
            {
                throw new InvalidOperationException(
                    $"Executor {executorType} must have attribute TaskExecutorAttribute");
            }

            var eventType = executorType.GetInterfaces()
                .First(i => i.IsGenericType && typeof(ITaskExecutor).IsAssignableFrom(i)).GenericTypeArguments
                .First();
            var registration = new ExecutorRegistration(executorType, eventType, groupInfo.Value.GroupId,
                groupInfo.Value.ParallelThreadCount, groupInfo.Value.BufferSize);
            executors.Add(registration);
        }

        services.Scan(selector => selector.FromTypes(executors.Select(e => e.ExecutorType)).AsSelfWithInterfaces()
            .WithScopedLifetime());

        services.AddScoped<ITaskScheduler, TTaskScheduler>();

        services.AddScoped<TasksManager>();
        services.AddTransient<IRepository<TBaseTask, Guid>, TasksRepository<TBaseTask, TDbContext>>();
        services.AddTransient<ITaskRepository<TBaseTask>, TasksRepository<TBaseTask, TDbContext>>();
        services.AddHostedService<TasksCleaner<TBaseTask, TOptions>>();
        services.AddHostedService<TasksMaintenance<TBaseTask, TOptions>>();

        ConfigureServicesInternal(applicationContext, services, startupOptions, executors);
        startupOptions.ConfigureServices(services);
    }

    protected abstract void ConfigureServicesInternal(IApplicationContext applicationContext,
        IServiceCollection services, TOptions startupOptions,
        List<ExecutorRegistration> executors);
}

public abstract class
    TasksModuleOptionsValidator<TBaseTask, TDbContext, TOptions> : AbstractValidator<TOptions>
    where TOptions : TasksModuleOptions<TBaseTask, TDbContext>
    where TBaseTask : BaseTask
    where TDbContext : TasksDbContext<TBaseTask>
{
    protected TasksModuleOptionsValidator() => RuleFor(o => o.HasJobs).Equal(true)
        .WithMessage("Необходимо сконфигурировать хотя бы одну задачу");
}
