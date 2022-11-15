using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Consul.Web;

public class ConsulWebHealthCheck : IHealthCheck
{
    private readonly ConsulWebClient consulClient;

    public ConsulWebHealthCheck(ConsulWebClient consulClient) => this.consulClient = consulClient;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        consulClient.CheckHealthAsync(cancellationToken);
}

