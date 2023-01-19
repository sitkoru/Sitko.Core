using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Grpc.Server.Discovery;

public class GrpcServiceHealthCheck<TService> : IHealthCheck where TService : class
{
    private readonly IGrpcServicesRegistrar registrar;

    public GrpcServiceHealthCheck(IGrpcServicesRegistrar registrar) => this.registrar = registrar;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        registrar.CheckHealthAsync<TService>(cancellationToken);
}

