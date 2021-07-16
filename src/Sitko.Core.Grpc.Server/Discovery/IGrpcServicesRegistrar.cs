namespace Sitko.Core.Grpc.Server.Discovery
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    public interface IGrpcServicesRegistrar
    {
        Task RegisterAsync<T>() where T : class;

        Task<HealthCheckResult> CheckHealthAsync<T>(CancellationToken cancellationToken = default) where T : class;
    }
}
