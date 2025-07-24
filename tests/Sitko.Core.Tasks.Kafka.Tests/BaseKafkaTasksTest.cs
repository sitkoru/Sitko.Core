using Cronos;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Tasks.Kafka.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Tasks.Kafka.Tests;

public abstract class BaseKafkaTasksTest : BaseTest<BaseKafkaTasksTestScope>
{
    protected BaseKafkaTasksTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }
}

public class BaseKafkaTasksTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.AddKafkaTasks<BaseTestTask, TestDbContext>(options =>
        {
            options
                .AddTask<TestTask, TestTaskConfig, TestTaskResult>(CronExpression.Parse("* * * * *"))
                .AddExecutorsFromAssemblyOf<TestTask>();
        });
        return hostBuilder;
    }
}
