namespace Sitko.Core.Grpc.Client
{
    using System.Threading;
    using System.Threading.Tasks;
    using Discovery;
    using global::Grpc.Core;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    public class GrpcClientHealthCheck<TClient> : IHealthCheck where TClient : ClientBase<TClient>
    {
        private readonly IGrpcServiceAddressResolver<TClient> resolver;

        public GrpcClientHealthCheck(IGrpcServiceAddressResolver<TClient> resolver) => this.resolver = resolver;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HealthCheckResult result;
            var uri = resolver.GetAddress();
            result = uri != null ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Empty url");

            return Task.FromResult(result);
        }
    }
}
