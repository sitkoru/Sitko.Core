﻿using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Server.Tests
{
    public class TestServiceImpl : TestService.TestServiceBase
    {
        public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
        {
            Logger.LogDebug("Execute request");
            return ProcessCall<TestRequest, TestResponse>(request, context, _ => new GrpcCallResult());
        }

        public TestServiceImpl(ILogger<TestServiceImpl> logger) : base(logger)
        {
        }
    }
}
