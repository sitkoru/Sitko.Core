using Elastic.Apm.Api;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Execution;

internal class TaskExecutorContext<TTask> : ITaskExecutorContext<TTask> where TTask : class, IBaseTask
{
    public IServiceScopeFactory ServiceScopeFactory { get; }
    public IRepository<TTask, Guid> Repository { get; }
    public ITracer? Tracer { get; }

    public TaskExecutorContext(IServiceScopeFactory serviceScopeFactory, IRepository<TTask, Guid> repository,
        ITracer? tracer = null)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Repository = repository;
        Tracer = tracer;
    }
}
