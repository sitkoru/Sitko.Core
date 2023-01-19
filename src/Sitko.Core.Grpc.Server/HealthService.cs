using Grpc.Core;
using Grpc.Health.V1;

namespace Sitko.Core.Grpc.Server;

public class HealthService : Health.HealthBase
{
    public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context) =>
        Task.FromResult(new HealthCheckResponse { Status = HealthCheckResponse.Types.ServingStatus.Serving });
}

