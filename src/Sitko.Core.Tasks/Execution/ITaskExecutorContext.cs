using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Execution;

public interface ITaskExecutorContext<TTask> where TTask : class, IBaseTask
{
    IServiceScopeFactory ServiceScopeFactory { get; }
    IRepository<TTask, Guid> Repository { get; }
}
