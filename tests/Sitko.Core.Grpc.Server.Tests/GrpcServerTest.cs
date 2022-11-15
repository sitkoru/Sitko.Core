using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Grpc.Server.Tests;

public class GrpcServerTest : BaseTest
{
    public GrpcServerTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TestResponse()
    {
        var application = new TestApplication(Array.Empty<string>());
        using var host = await application.StartAsync();
        var service = host.GetTestServer();
        var responseVersionHandler = new ResponseVersionHandler { InnerHandler = service.CreateHandler() };
        var client = new HttpClient(responseVersionHandler) { BaseAddress = new Uri("http://localhost") };
        using var channel = GrpcChannel.ForAddress(client.BaseAddress,
            new GrpcChannelOptions { HttpClient = client });
        var grpcClient = new TestService.TestServiceClient(channel);

        var response = await grpcClient.RequestAsync(new TestRequest());
        Assert.True(response.ResponseInfo.IsSuccess);
    }

    private sealed class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Version = request.Version;

            return response;
        }
    }
}

