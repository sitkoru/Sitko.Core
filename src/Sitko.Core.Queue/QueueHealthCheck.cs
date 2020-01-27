using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Queue
{
    public class QueueHealthCheck : IHealthCheck
    {
        private readonly IQueue _queue;

        public QueueHealthCheck(IQueue queue)
        {
            _queue = queue;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            (HealthStatus status, string? errorMessage) = await _queue.CheckHealthAsync();
            return new HealthCheckResult(status, errorMessage);
        }
    }
}
