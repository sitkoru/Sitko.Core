using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Health.Telegram.Tests;

public class TelegramPublisherTest : BaseTest<TelegramPublisherTestScope>
{
    public TelegramPublisherTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Healthy()
    {
        var scope = await GetScopeAsync();
        var publisher = scope.GetService<IHealthCheckPublisher>();

        var failReport =
            new HealthReport(
                new Dictionary<string, HealthReportEntry>
                {
                    {
                        "test", new HealthReportEntry(HealthStatus.Unhealthy, "Something is broken",
                            TimeSpan.FromMinutes(1),
                            null,
                            new Dictionary<string, object>())
                    }
                }, TimeSpan.FromMinutes(1));
        await publisher.PublishAsync(failReport, CancellationToken.None);

        var report =
            new HealthReport(
                new Dictionary<string, HealthReportEntry>
                {
                    {
                        "test", new HealthReportEntry(HealthStatus.Healthy, "All is good",
                            TimeSpan.FromMinutes(1),
                            null,
                            new Dictionary<string, object>())
                    }
                }, TimeSpan.FromMinutes(1));
        await publisher.PublishAsync(report, CancellationToken.None);
    }

    [Fact]
    public async Task UnHealthy()
    {
        var scope = await GetScopeAsync();
        var publisher = scope.GetService<IHealthCheckPublisher>();

        try
        {
            throw new InvalidOperationException("Mega exception");
        }
        catch (Exception ex)
        {
            var report =
                new HealthReport(
                    new Dictionary<string, HealthReportEntry>
                    {
                        {
                            "test", new HealthReportEntry(HealthStatus.Unhealthy, "All bad",
                                TimeSpan.FromMinutes(1),
                                ex,
                                new Dictionary<string, object>())
                        }
                    }, TimeSpan.FromMinutes(1));
            await publisher.PublishAsync(report, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Degraded()
    {
        var scope = await GetScopeAsync();
        var publisher = scope.GetService<IHealthCheckPublisher>();

        var report =
            new HealthReport(
                new Dictionary<string, HealthReportEntry>
                {
                    {
                        "test", new HealthReportEntry(HealthStatus.Degraded, "Something is not good",
                            TimeSpan.FromMinutes(1),
                            null,
                            new Dictionary<string, object>())
                    }
                }, TimeSpan.FromMinutes(1));
        await publisher.PublishAsync(report, CancellationToken.None);
    }
}

public class TelegramPublisherTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name).AddTelegramHealthReporter();
}
