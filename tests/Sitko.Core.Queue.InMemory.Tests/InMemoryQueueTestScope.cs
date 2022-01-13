using Microsoft.Extensions.Configuration;
using Sitko.Core.App;
using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.InMemory.Tests;

public class
    InMemoryQueueTestScope : BaseQueueTestScope<InMemoryQueueModule, InMemoryQueue, InMemoryQueueModuleOptions>
{
    protected override void Configure(IConfiguration configuration, IAppEnvironment environment,
        InMemoryQueueModuleOptions options,
        string name)
    {
    }
}
