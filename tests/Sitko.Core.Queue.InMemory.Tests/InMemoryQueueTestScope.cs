using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.InMemory.Tests
{
    public class
        InMemoryQueueTestScope : BaseQueueTestScope<InMemoryQueueModule, InMemoryQueue, InMemoryQueueModuleConfig>
    {
        protected override InMemoryQueueModuleConfig CreateConfig(IConfiguration configuration,
            IHostEnvironment environment, string name)
        {
            return new InMemoryQueueModuleConfig();
        }
    }
}
