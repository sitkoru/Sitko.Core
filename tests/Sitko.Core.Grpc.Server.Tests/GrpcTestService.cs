﻿using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Server.Tests;

public class GrpcTestService : TestService.TestServiceBase
{
    public GrpcTestService(ILogger<GrpcTestService> logger) : base(logger)
    {
    }

    public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
    {
        Logger.LogDebug("Execute request");
        return ProcessCall<TestResponse>(request, context, _ => new GrpcCallResult());
    }
}

