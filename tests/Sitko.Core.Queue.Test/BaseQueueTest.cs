using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Tests
{
    public abstract class BaseQueueTest<T, TQueueModule, TQueue, TConfig> : BaseTest<T>
        where T : BaseQueueTestScope<TQueueModule, TQueue, TConfig>
        where TQueueModule : QueueModule<TQueue, TConfig>
        where TQueue : class, IQueue
        where TConfig : QueueModuleConfig
    {
        protected BaseQueueTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }

    public abstract class BaseQueueTestScope<TQueueModule, TQueue, TConfig> : BaseTestScope
        where TQueueModule : QueueModule<TQueue, TConfig>
        where TQueue : class, IQueue
        where TConfig : QueueModuleConfig
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name);
            application.AddModule<TQueueModule, TConfig>((
                configuration, environment) =>
            {
                var config = CreateConfig(configuration, environment, name);
                return config;
            });

            return application;
        }

        protected abstract TConfig CreateConfig(IConfiguration configuration, IHostEnvironment environment, string name);
    }

    public abstract class
        BaseTestQueueTest<T> : BaseQueueTest<T, TestQueueModule, TestQueue, TestQueueConfig>
        where T : BaseQueueTestScope<TestQueueModule, TestQueue, TestQueueConfig>
    {
        protected BaseTestQueueTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
    }

    public class BaseTestQueueTestScope : BaseQueueTestScope<TestQueueModule, TestQueue, TestQueueConfig>
    {
        protected override TestQueueConfig CreateConfig(IConfiguration configuration, IHostEnvironment environment, string name)
        {
            var config = new TestQueueConfig();
            ConfigureQueue(config, configuration, environment, name);
            return config;
        }

        protected virtual void ConfigureQueue(TestQueueConfig config, IConfiguration configuration,
            IHostEnvironment environment, string name)
        {
        }
    }
}
