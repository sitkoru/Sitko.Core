using Sitko.Core.Tasks.Kafka.Tests.Data;
using Sitko.Core.Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Tasks.Kafka.Tests;

public abstract class BaseKafkaTasksTest : BaseTest<BaseKafkaTasksTestScope>
{
    protected BaseKafkaTasksTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}

public class BaseKafkaTasksTestScope : BaseTestScope
{
    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base
            .ConfigureApplication(application, name)
            .AddKafkaTasks<BaseTestTask, TestDbContext>(options =>
            {
                options
                    .AddTask<TestTask, TestTaskConfig, TestTaskResult>("* * * * *")
                    .AddExecutorsFromAssemblyOf<TestTask>();
            });
        return application;
    }
}
