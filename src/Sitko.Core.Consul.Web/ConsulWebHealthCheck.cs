using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Consul.Web
{
    public class ConsulWebHealthCheck : IHealthCheck
    {
        private readonly ConsulWebClient _consulClient;

        public ConsulWebHealthCheck(ConsulWebClient consulClient)
        {
            _consulClient = consulClient;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _consulClient.CheckHealthAsync(cancellationToken);
        }
    }
}