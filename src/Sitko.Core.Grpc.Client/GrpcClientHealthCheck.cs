using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client;

public class GrpcClientHealthCheck<TClient> : IHealthCheck where TClient : ClientBase<TClient>
{
    private readonly IGrpcServiceAddressResolver<TClient> resolver;

    public GrpcClientHealthCheck(IGrpcServiceAddressResolver<TClient> resolver) => this.resolver = resolver;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        HealthCheckResult result;
        var uri = resolver.GetAddress();
        result = uri != null ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Empty url");

        return Task.FromResult(result);
    }
}

