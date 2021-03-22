using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Server.Tests
{
    public class TestServiceImpl : TestService.TestServiceBase
    {
        public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
        {
            Logger.LogDebug("Execute request");
            var result = ProcessCall<TestRequest, TestResponse>(request, _ => new GrpcCallResult());
            return Task.FromResult(result);
        }

        public TestServiceImpl(ILogger<TestServiceImpl> logger) : base(logger)
        {
        }
    }
}