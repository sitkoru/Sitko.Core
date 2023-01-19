using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Queue;

public class QueueHealthCheck : IHealthCheck
{
    private readonly IQueue queue;

    public QueueHealthCheck(IQueue queue) => this.queue = queue;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var (status, errorMessage) = await queue.CheckHealthAsync();
        return new HealthCheckResult(status, errorMessage);
    }
}

