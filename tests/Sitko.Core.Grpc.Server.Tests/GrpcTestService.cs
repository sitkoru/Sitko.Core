using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Server.Tests;

public class GrpcTestService : TestService.TestServiceBase
{
    private readonly IGrpcCallProcessor<GrpcTestService> grpcCallProcessor;

    public GrpcTestService(ILogger<GrpcTestService> logger, IGrpcCallProcessor<GrpcTestService> grpcCallProcessor) : base(logger) => this.grpcCallProcessor = grpcCallProcessor;

    public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
    {
        Logger.LogDebug("Execute request");
        return grpcCallProcessor.ProcessCall<TestResponse>(request, context, _ => new GrpcCallResult());
    }
}

