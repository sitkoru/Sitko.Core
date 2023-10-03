using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks;

public abstract class
    TasksModule<TBaseTask, TDbContext, TOptions> : BaseApplicationModule<TOptions>
    where TOptions : TasksModuleOptions<TBaseTask, TDbContext>, new()
    where TDbContext : TasksDbContext<TBaseTask>
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
                    $"Consumer {executorType} must have attribute TaskExecutorAttribute");
            }

            var eventType = executorType.GetInterfaces()
                .First(i => i.IsGenericType && typeof(ITaskExecutor).IsAssignableFrom(i)).GenericTypeArguments
                .First();
            var registration = new ExecutorRegistration(executorType, eventType, groupInfo.Value.GroupId,
                groupInfo.Value.ParallelThreadCount, groupInfo.Value.BufferSize);
            executors.Add(registration);
        }

        ConfigureServicesInternal(applicationContext, services, startupOptions, executors);
    }

    protected abstract void ConfigureServicesInternal(IApplicationContext applicationContext,
        IServiceCollection services, TOptions startupOptions,
        List<ExecutorRegistration> executors);
}

public class TasksModuleOptions
{
    public bool IsAllTasksDisabled { get; set; }
    public string[] DisabledTasks { get; set; } = Array.Empty<string>();

    public int? AllTasksRetentionDays { get; set; }
    public Dictionary<string, int> RetentionDays { get; set; } = new();
}

public abstract class TasksModuleOptions<TBaseTask, TDbContext> : BaseModuleOptions, IModuleOptionsWithValidation
    where TDbContext : TasksDbContext<TBaseTask> where TBaseTask : BaseTask
{
    public List<Assembly> Assemblies { get; } = new();

    public TasksModuleOptions<TBaseTask, TDbContext> AddExecutorsFromAssemblyOf<TAssembly>()
    {
        Assemblies.Add(typeof(TAssembly).Assembly);
        return this;
    }

    internal bool HasJobs => false;

    public TasksModuleOptions<TBaseTask, TDbContext> AddTask<TTask, TConfig, TResult>(TimeSpan interval)
        where TTask : class, IBaseTask<TConfig, TResult>
        where TConfig : BaseTaskConfig, new()
        where TResult : BaseTaskResult, new()
    {
        return this;
    }

    public abstract Type GetValidatorType();
}

public abstract class TasksDbContext<TBaseTask> : BaseDbContext where TBaseTask : BaseTask
{
    private readonly DbContextOptions options;
    protected TasksDbContext(DbContextOptions options) : base(options) => this.options = options;

    private DbSet<TBaseTask> Tasks => Set<TBaseTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var discriminatorBuilder = modelBuilder.Entity<TBaseTask>().HasDiscriminator<string>(nameof(BaseTask.Type));

        var tasksExtension = options.FindExtension<TasksDbContextOptionsExtension<TBaseTask>>();
        if (tasksExtension is not null)
        {
            tasksExtension.Configure(modelBuilder, discriminatorBuilder);
        }

        modelBuilder.Entity<TBaseTask>().Property(task => task.Queue).HasDefaultValue("default");
    }
}

internal class TasksDbContextOptionsExtension<TBaseTask> : IDbContextOptionsExtension where TBaseTask : BaseTask
{
    private readonly List<Action<ModelBuilder, DiscriminatorBuilder<string>>> discriminatorConfigurations = new();
    public void ApplyServices(IServiceCollection services) { }

    public void Validate(IDbContextOptions options) { }

    public DbContextOptionsExtensionInfo Info => new TasksDbContextOptionsExtensionInfo<TBaseTask>(this);

    public void Register<TTask, TConfig, TResult>() where TTask : class, IBaseTask<TConfig, TResult>
        where TConfig : BaseTaskConfig, new()
        where TResult : BaseTaskResult =>
        discriminatorConfigurations.Add((modelBuilder, discriminatorBuilder) =>
        {
            modelBuilder.Entity<TTask>().Property(task => task.Config).HasColumnType("jsonb")
                .HasColumnName(nameof(IBaseTask<TConfig, TResult>.Config));
            modelBuilder.Entity<TTask>().Property(task => task.Result).HasColumnType("jsonb")
                .HasColumnName(nameof(IBaseTask<TConfig, TResult>.Result));
            var attr = typeof(TTask).GetCustomAttributes(typeof(TaskAttribute), true).Cast<TaskAttribute>()
                .FirstOrDefault();
            discriminatorBuilder.HasValue<TTask>(attr is null ? typeof(TTask).Name : attr.Key);
        });

    public void Configure(ModelBuilder modelBuilder, DiscriminatorBuilder<string> discriminatorBuilder)
    {
        foreach (var discriminatorConfiguration in discriminatorConfigurations)
        {
            discriminatorConfiguration(modelBuilder, discriminatorBuilder);
        }
    }
}

internal class TasksDbContextOptionsExtensionInfo<TBaseTask> : DbContextOptionsExtensionInfo where TBaseTask : BaseTask
{
    public TasksDbContextOptionsExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
    {
    }

#if NET6_0_OR_GREATER
    public override int GetServiceProviderHashCode() => Extension.GetHashCode();
#else
    public override long GetServiceProviderHashCode() => Extension.GetHashCode();
#endif

#if NET6_0_OR_GREATER
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
#endif
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
    }

    public override bool IsDatabaseProvider => false;
    public override string LogFragment => nameof(TasksDbContextOptionsExtension<TBaseTask>);
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
