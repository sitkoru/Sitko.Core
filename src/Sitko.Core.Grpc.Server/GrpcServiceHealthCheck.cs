using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServiceHealthCheck<TService> : IHealthCheck where TService : class
    {
        private readonly IGrpcServicesRegistrar _registrar;

        public GrpcServiceHealthCheck(IGrpcServicesRegistrar registrar)
        {
            _registrar = registrar;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var isRegistered = await _registrar.IsRegistered<TService>();
            return isRegistered ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
    }
}
