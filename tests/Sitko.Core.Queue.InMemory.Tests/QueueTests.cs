using Sitko.Core.Queue.Tests;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.InMemory.Tests
{
    public class BasicQueueTests : BasicQueueTests<InMemoryQueueTestScope, InMemoryQueueModule, InMemoryQueue,
        InMemoryQueueModuleConfig>
    {
        public BasicQueueTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }
}
