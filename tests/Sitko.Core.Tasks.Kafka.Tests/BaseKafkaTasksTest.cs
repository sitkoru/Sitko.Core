using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Tasks.Kafka.Tests.Data;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Tasks.Kafka.Tests
{
    public abstract class BaseKafkaTasksTest(ITestOutputHelper testOutputHelper)
        : BaseTest<BaseKafkaTasksTestScope>(testOutputHelper);

    public class BaseKafkaTasksTestScope : BaseTestScope
    {
        protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
        {
            _ = base.ConfigureApplication(hostBuilder, name);
            _ = hostBuilder.Services.AddSingleton<TaskExecutionControl>();
            _ = hostBuilder.AddEFRepositories();
            _ = hostBuilder.AddKafkaTasks<BaseTestTask, TestDbContext>(options =>
            {
                options.TasksTopic = "lifecycle";
                options.TopicPrefix = Id.ToString("N");
                options.ConsumerGroupPrefix = Id.ToString("N");
                options.IsAllTasksDisabled = true;
                _ = options.AddTask<TestTask, TestTaskConfig, TestTaskResult>("* * * * *")
                    .AddExecutorsFromAssemblyOf<TestTask>();
            }, configurePostgres: true, configurePostgresAction: options =>
            {
                options.Host = KafkaFixture.Postgres.Hostname;
                options.Port = KafkaFixture.Postgres.GetMappedPublicPort(5432);
                options.Password = "tasks-tests";
                options.Database = $"tasks_{Id:N}";
                options.AutoApplyMigrations = false;
                options.EnableNpgsqlPooling = false;
            }, configureKafkaAction: static options =>
            {
                options.Brokers = [KafkaFixture.Kafka.GetBootstrapAddress()];
                options.AutoOffsetReset = AutoOffsetReset.Latest;
                options.WaitForConsumerAssignments = true;
                options.TopicPartitionsCount = 1;
            });
            return hostBuilder;
        }

        public override async Task OnCreatedAsync()
        {
            _ = await GetService<TestDbContext>().Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            await StartApplicationAsync(TestContext.Current.CancellationToken);
            await base.OnCreatedAsync();
        }
    }
}
