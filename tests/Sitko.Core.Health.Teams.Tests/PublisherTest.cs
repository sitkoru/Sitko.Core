using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Health.Teams.Tests
{
    public class TeamsPublisherTest : BaseTest<TeamsPublisherTestScope>
    {
        public TeamsPublisherTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Healthy()
        {
            var scope = await GetScopeAsync();
            var publisher = GetPublisher(scope);

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
            var publisher = GetPublisher(scope);

            try
            {
                throw new Exception("Mega exception");
            }
            catch (Exception ex)
            {
                var report =
                    new HealthReport(
                        new Dictionary<string, HealthReportEntry>
                        {
                            {
                                "test", new HealthReportEntry(HealthStatus.Unhealthy, "All bad", TimeSpan.FromMinutes(1),
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
            var publisher = GetPublisher(scope);

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

        private static TeamsHealthCheckPublisher GetPublisher(TeamsPublisherTestScope scope)
        {
            var logger = scope.GetLogger<TeamsHealthCheckPublisher>();
            var env = scope.Get<IHostEnvironment>();
            var httpFactory = scope.Get<IHttpClientFactory>();
            var options = scope.Get<TeamsHealthCheckPublisherOptions>();
            var publisher =
                new TeamsHealthCheckPublisher(options, logger, env, httpFactory);
            return publisher;
        }
    }

    public class TeamsPublisherTestScope : BaseTestScope
    {
        protected override IServiceCollection ConfigureServices(IConfiguration configuration,
            IHostEnvironment environment,
            IServiceCollection services, string name)
        {
            services.AddSingleton(
                new TeamsHealthCheckPublisherOptions {WebHookUrl = configuration["TEAMS_WEBHOOK_URL"]});
            services.AddHttpClient();
            return base.ConfigureServices(configuration, environment, services, name);
        }
    }
}
