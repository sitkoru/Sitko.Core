using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Web;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Grpc.Server.Tests
{
    public class GrpcServerTest : BaseTest
    {
        public GrpcServerTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task TestResponse()
        {
            var application = new TestApplication(new string[0]);
            using var host = await application.GetHostBuilder().ConfigureWebHostDefaults(builder =>
            {
                builder.UseTestServer();
            }).StartAsync();
            var service = host.GetTestServer();
            var responseVersionHandler = new ResponseVersionHandler {InnerHandler = service.CreateHandler()};
            var client = new HttpClient(responseVersionHandler) {BaseAddress = new Uri("http://localhost")};
            using var channel = GrpcChannel.ForAddress(client.BaseAddress,
                new GrpcChannelOptions {HttpClient = client});
            var grpcClient = new TestService.TestServiceClient(channel);

            var response = await grpcClient.RequestAsync(new TestRequest());
            Assert.True(response.ResponseInfo.IsSuccess);
        }

        private class ResponseVersionHandler : DelegatingHandler
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

    public class TestStartup : BaseStartup<TestApplication>
    {
        public TestStartup(IConfiguration configuration, IHostEnvironment environment) : base(
            configuration, environment)
        {
        }
    }

    public class TestServiceImpl : TestService.TestServiceBase
    {
        public override Task<TestResponse> Request(TestRequest request, ServerCallContext context)
        {
            return Task.FromResult(new TestResponse {ResponseInfo = new ApiResponseInfo {IsSuccess = true}});
        }
    }

    public class TestApplication : WebApplication<TestApplication>
    {
        public TestApplication(string[] args) : base(args)
        {
            AddModule<GrpcServerModule, GrpcServerOptions>((configuration, environment, moduleConfig) =>
            {
                moduleConfig.RegisterService<TestServiceImpl>();
            }).UseStartup<TestStartup>();
        }
    }
}
