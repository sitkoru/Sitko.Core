using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Server.Tests
{
    public static partial class TestService
    {
        public abstract partial class TestServiceBase : GrpcServiceBase
        {
            protected TestServiceBase(ILogger<TestServiceBase> logger) : base(logger)
            {
            }
        }
    }
}