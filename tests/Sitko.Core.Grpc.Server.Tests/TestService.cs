using Microsoft.Extensions.Logging;

namespace Sitko.Core.Grpc.Server.Tests;

public static partial class TestService
{
    public abstract partial class TestServiceBase
    {
        protected ILogger<TestServiceBase> Logger { get; }

        protected TestServiceBase(ILogger<TestServiceBase> logger) => Logger = logger;
    }
}
