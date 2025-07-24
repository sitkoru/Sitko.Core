using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Grpc.Server.Discovery;

public interface IGrpcServicesRegistrar
{
    Task RegisterAsync<T>(CancellationToken cancellationToken = default) where T : class;

    Task<HealthCheckResult> CheckHealthAsync<T>(CancellationToken cancellationToken = default) where T : class;
}
