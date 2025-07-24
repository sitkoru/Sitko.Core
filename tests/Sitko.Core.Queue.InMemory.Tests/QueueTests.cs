using Sitko.Core.Queue.Tests;
using Xunit;

namespace Sitko.Core.Queue.InMemory.Tests;

public class BasicQueueTests : BasicQueueTests<InMemoryQueueTestScope, InMemoryQueueModule, InMemoryQueue,
    InMemoryQueueModuleOptions>
{
    public BasicQueueTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}
