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

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _registrar.CheckHealthAsync<TService>(cancellationToken);
        }
    }
}
