namespace Sitko.Core.Grpc.Server
{
    using System.Threading.Tasks;
    using global::Grpc.Core;
    using global::Grpc.Health.V1;

    public class HealthService : Health.HealthBase
    {
        public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context) =>
            Task.FromResult(new HealthCheckResponse {Status = HealthCheckResponse.Types.ServingStatus.Serving});
    }
}
