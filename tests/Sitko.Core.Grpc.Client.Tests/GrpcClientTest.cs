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
        var data = Guid.NewGuid().ToString();
        var result = await client.RequestAsync(new TestRequest { Data = data });
        result.ResponseInfo.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(data);
    }
}
