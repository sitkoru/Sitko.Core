using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Consul;

public class ConsulHealthCheck : IHealthCheck
{
    private readonly IConsulClientProvider consulClientProvider;

    public ConsulHealthCheck(IConsulClientProvider consulClientProvider) =>
        this.consulClientProvider = consulClientProvider;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var leader = await consulClientProvider.Client.Status.Leader(cancellationToken);
            return string.IsNullOrEmpty(leader)
                ? HealthCheckResult.Degraded("Empty leader response from Consul")
                : HealthCheckResult.Healthy($"Leader: {leader}");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Error checking Consul leader status", exception);
        }
    }
}

