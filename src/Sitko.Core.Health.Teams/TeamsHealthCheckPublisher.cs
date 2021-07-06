using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamsHook.NET;

namespace Sitko.Core.Health.Teams
{
    public class TeamsHealthCheckPublisher : BaseHealthCheckPublisher<TeamsHealthReporterModuleOptions>
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TeamsHealthCheckPublisher(IOptionsMonitor<TeamsHealthReporterModuleOptions> options,
            ILogger<TeamsHealthCheckPublisher> logger, IHostEnvironment hostingEnvironment,
            IHttpClientFactory httpClientFactory) : base(options, logger, hostingEnvironment)
        {
            _httpClientFactory = httpClientFactory;
        }

        protected override Task DoSendAsync(string checkName, HealthReportEntry entry,
            CancellationToken cancellationToken)
        {
            var serviceName = $"{HostingEnvironment.ApplicationName} ({Dns.GetHostName()})";
            string title = entry.Status switch
            {
                HealthStatus.Unhealthy => $"Error in {serviceName}. Check: {checkName}",
                HealthStatus.Degraded => $"Check {checkName} in {serviceName} is degraded",
                HealthStatus.Healthy => $"Check {checkName} in {serviceName} is restored",
                _ => $"Unknown check {checkName} status {entry.Status}"
            };
            string color = entry.Status switch
            {
                HealthStatus.Unhealthy => Options.UnHealthyColor,
                HealthStatus.Degraded => Options.DegradedColor,
                HealthStatus.Healthy => Options.HealthyColor,
                _ => ""
            };
            string summary = string.IsNullOrEmpty(entry.Description) ? "No description" : entry.Description;
            var sections = new List<Section>();
            if (entry.Status == HealthStatus.Unhealthy || entry.Status == HealthStatus.Degraded)
            {
                if (entry.Exception != null)
                {
                    sections.Add(new Section {ActivityTitle = "Exception", ActivityText = $"```{entry.Exception}```"});
                }
            }

            var client = new TeamsHookClient(_httpClientFactory.CreateClient());
            return client.PostAsync(Options.WebHookUrl,
                new MessageCard {Title = title, Text = summary, Sections = sections, ThemeColor = color});
        }
    }
}
