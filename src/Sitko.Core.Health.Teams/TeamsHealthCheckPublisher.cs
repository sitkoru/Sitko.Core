using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using TeamsHook.NET;

namespace Sitko.Core.Health.Teams;

public class TeamsHealthCheckPublisher : BaseHealthCheckPublisher<TeamsHealthReporterModuleOptions>
{
    private readonly IHttpClientFactory httpClientFactory;

    public TeamsHealthCheckPublisher(IOptionsMonitor<TeamsHealthReporterModuleOptions> options,
        ILogger<TeamsHealthCheckPublisher> logger, IApplicationContext applicationContext,
        IHttpClientFactory httpClientFactory) : base(options, logger, applicationContext) =>
        this.httpClientFactory = httpClientFactory;

    protected override Task DoSendAsync(string checkName, HealthReportEntry entry,
        CancellationToken cancellationToken)
    {
        var serviceName = $"{ApplicationContext.Name} ({Dns.GetHostName()})";
        var title = entry.Status switch
        {
            HealthStatus.Unhealthy => $"Error in {serviceName}. Check: {checkName}",
            HealthStatus.Degraded => $"Check {checkName} in {serviceName} is degraded",
            HealthStatus.Healthy => $"Check {checkName} in {serviceName} is restored",
            _ => $"Unknown check {checkName} status {entry.Status}"
        };
        var color = entry.Status switch
        {
            HealthStatus.Unhealthy => Options.UnHealthyColor,
            HealthStatus.Degraded => Options.DegradedColor,
            HealthStatus.Healthy => Options.HealthyColor,
            _ => ""
        };
        var summary = string.IsNullOrEmpty(entry.Description) ? "No description" : entry.Description;
        var sections = new List<Section>();
        if (entry.Status == HealthStatus.Unhealthy || entry.Status == HealthStatus.Degraded)
        {
            if (entry.Exception != null)
            {
                sections.Add(new Section { ActivityTitle = "Exception", ActivityText = $"```{entry.Exception}```" });
            }
        }

        var client = new TeamsHookClient(httpClientFactory.CreateClient());
        return client.PostAsync(Options.WebHookUrl,
            new MessageCard { Title = title, Text = summary, Sections = sections, ThemeColor = color });
    }
}

