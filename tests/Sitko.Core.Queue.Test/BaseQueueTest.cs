using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Queue.Tests;

public abstract class BaseQueueTest<T, TQueueModule, TQueue, TConfig> : BaseTest<T>
    where T : BaseQueueTestScope<TQueueModule, TQueue, TConfig>
    where TQueueModule : QueueModule<TQueue, TConfig>, new()
    where TQueue : class, IQueue
    where TConfig : QueueModuleOptions, new()
{
    protected BaseQueueTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}

public abstract class BaseQueueTestScope<TQueueModule, TQueue, TQueueModuleOptions> : BaseTestScope
    where TQueueModule : QueueModule<TQueue, TQueueModuleOptions>, new()
    where TQueue : class, IQueue
    where TQueueModuleOptions : QueueModuleOptions, new()
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.GetSitkoCore().AddModule<TQueueModule, TQueueModuleOptions>((
            applicationContext, moduleOptions) => Configure(applicationContext, moduleOptions, name));
        return hostBuilder;
    }

    protected abstract void Configure(IApplicationContext applicationContext, TQueueModuleOptions options,
        string name);
}

public abstract class
    BaseTestQueueTest<T> : BaseQueueTest<T, TestQueueModule, TestQueue, TestQueueOptions>
    where T : BaseQueueTestScope<TestQueueModule, TestQueue, TestQueueOptions>
{
    protected BaseTestQueueTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}

public abstract class BaseTestQueueTestScope : BaseQueueTestScope<TestQueueModule, TestQueue, TestQueueOptions>;
