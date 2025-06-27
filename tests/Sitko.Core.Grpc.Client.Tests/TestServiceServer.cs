using Grpc.Core;
using Sitko.Core.Grpc.Server;

namespace Sitko.Core.Grpc.Client.Tests;

public class TestServiceServer(IGrpcCallProcessor<TestServiceServer> grpcCallProcessor) : TestService.TestServiceBase
{
    public override Task<TestResponse> Request(TestRequest request, ServerCallContext context) =>
        grpcCallProcessor.ProcessCall<TestResponse>(request, context, response =>
        {
            response.Data = request.Data;
            return GrpcCallResult.Ok();
        });
}
