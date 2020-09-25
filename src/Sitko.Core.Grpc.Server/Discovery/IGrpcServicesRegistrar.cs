using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sitko.Core.Grpc.Server.Discovery
{
    public interface IGrpcServicesRegistrar
    {
        Task RegisterAsync<T>() where T : class;

        Task<HealthCheckResult> CheckHealthAsync<T>(
            CancellationToken cancellationToken = new CancellationToken()) where T : class;
    }
}
