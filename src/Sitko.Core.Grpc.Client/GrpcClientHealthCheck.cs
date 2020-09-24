using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client
{
    public class GrpcClientHealthCheck<TClient> : IHealthCheck where TClient : ClientBase<TClient>
    {
        private readonly IGrpcServiceAddressResolver<TClient> _resolver;

        public GrpcClientHealthCheck(IGrpcServiceAddressResolver<TClient> resolver)
        {
            _resolver = resolver;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HealthCheckResult result;
            var uri = _resolver.GetAddress();
            if (uri != null)
            {
                result = HealthCheckResult.Healthy();
            }
            else
            {
                result = HealthCheckResult.Unhealthy("Empty url");
            }

            return Task.FromResult(result);
        }
    }
}
