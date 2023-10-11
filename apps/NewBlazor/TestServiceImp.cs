using Grpc.Core;
using Sitko.Core.Grpc;
using Sitko.Core.Grpc.Server.Tests;

namespace NewBlazor;

public class TestServiceImp : TestService.TestServiceBase
{
    public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
    {
        var result = new TestResponse { ResponseInfo = new ApiResponseInfo { IsSuccess = true, TotalItems = 100500 } };
        return Task.FromResult(result);
    }
}
