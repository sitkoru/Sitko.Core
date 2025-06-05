using FluentAssertions;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Grpc.Client.Tests;

public abstract class GrpcClientTest<TScope>(ITestOutputHelper testOutputHelper) : BaseTest<TScope>(testOutputHelper)
    where TScope : GrpcClientScope
{
    [Fact]
    public async Task GetResult()
    {
        var scope = await GetScopeAsync();
        var client = scope.GetService<TestService.TestServiceClient>();
        //await Task.Delay(TimeSpan.FromMinutes(1));
        var result = await client.RequestAsync(new TestRequest());
        result.ResponseInfo.IsSuccess.Should().BeTrue();
    }
}
