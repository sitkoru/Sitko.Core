using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit.Abstractions;

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
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddModule<TQueueModule, TQueueModuleOptions>((
            applicationContext, moduleOptions) => Configure(applicationContext, moduleOptions, name));

        return application;
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

public abstract class BaseTestQueueTestScope : BaseQueueTestScope<TestQueueModule, TestQueue, TestQueueOptions>
{
}
