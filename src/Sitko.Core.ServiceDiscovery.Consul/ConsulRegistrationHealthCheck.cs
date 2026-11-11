using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ServiceDiscovery.Consul
{
    internal sealed class ConsulRegistrationHealthCheck(IOptions<ServiceDiscoveryOptions> services) : IHealthCheck
    {
        private int registered;

        public void MarkRegistered() => Interlocked.Exchange(ref registered, 1);

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(services.Value.Services.Count == 0 || Volatile.Read(ref registered) == 1
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Consul registration has not completed"));
    }

}
