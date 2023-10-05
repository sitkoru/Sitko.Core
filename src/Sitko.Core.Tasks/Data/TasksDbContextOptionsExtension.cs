using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Data;

internal class TasksDbContextOptionsExtension<TBaseTask> : IDbContextOptionsExtension where TBaseTask : BaseTask
{
    public string TableName { get; init; } = "";

    private readonly List<Action<ModelBuilder, DiscriminatorBuilder<string>>> discriminatorConfigurations = new();
    public void ApplyServices(IServiceCollection services) { }

    public void Validate(IDbContextOptions options) { }

    public DbContextOptionsExtensionInfo Info => new TasksDbContextOptionsExtensionInfo<TBaseTask>(this);

    public void Register<TTask, TConfig, TResult>() where TTask : class, IBaseTask<TConfig, TResult>
        where TConfig : BaseTaskConfig, new()
        where TResult : BaseTaskResult =>
        discriminatorConfigurations.Add((modelBuilder, discriminatorBuilder) =>
        {
            modelBuilder.Entity<TTask>().ToTable(TableName);
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
        modelBuilder.Entity<TBaseTask>().ToTable(TableName);
        foreach (var discriminatorConfiguration in discriminatorConfigurations)
        {
            discriminatorConfiguration(modelBuilder, discriminatorBuilder);
        }
    }
}
