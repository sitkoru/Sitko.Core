using Microsoft.EntityFrameworkCore;
using Sitko.Core.Db.Postgres;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Data;

public abstract class TasksDbContext<TBaseTask> : BaseDbContext where TBaseTask : BaseTask
{
    private readonly DbContextOptions options;
    protected TasksDbContext(DbContextOptions options) : base(options) => this.options = options;

    // ReSharper disable once UnusedMember.Local
    private DbSet<TBaseTask> Tasks => Set<TBaseTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var discriminatorBuilder = modelBuilder.Entity<TBaseTask>().HasDiscriminator<string>(nameof(BaseTask.Type));

        var tasksExtension = options.FindExtension<TasksDbContextOptionsExtension<TBaseTask>>();
        tasksExtension?.Configure(modelBuilder, discriminatorBuilder);
    }
}
