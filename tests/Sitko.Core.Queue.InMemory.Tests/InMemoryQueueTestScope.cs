using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.InMemory.Tests
{
    public class
        InMemoryQueueTestScope : BaseQueueTestScope<InMemoryQueueModule, InMemoryQueue, InMemoryQueueModuleConfig>
    {
        protected override void Configure(IConfiguration configuration, IHostEnvironment environment,
            InMemoryQueueModuleConfig config,
            string name)
        {
        }
    }
}
