using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Sitko.Core.App;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class ProviderTests : BaseTest<GrpcClientScope>
    {
        public ProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task TestDI()
        {
            var scope = await GetScopeAsync();

            var resolver =
                (TestGrpcClientResolver<TestService.TestServiceClient>) scope
                    .Get<IGrpcServiceAddressResolver<TestService.TestServiceClient>>();
            resolver.SetPort(4000);
            var client1 = scope.Get<IGrpcClientProvider<TestService.TestServiceClient>>().Instance;
            Assert.NotNull(client1);
            Assert.Throws<RpcException>(() => client1.Request(new TestRequest()));
            resolver.SetPort(5000);
            var client2 = scope.Get<IGrpcClientProvider<TestService.TestServiceClient>>().Instance;
            Assert.NotNull(client2);
            Assert.Throws<RpcException>(() => client2.Request(new TestRequest()));
        }
    }

    public class test : Interceptor
    {
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return base.BlockingUnaryCall(request, context, continuation);
        }
    }
    
    public class GrpcClientScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<TestGrpcClientModule<TestService.TestServiceClient>, GrpcClientModuleConfig>(
                    (configuration, environment, moduleConfig) =>
                    {
                        moduleConfig.EnableHttp2UnencryptedSupport = true;
                        moduleConfig.DisableCertificatesValidation = true;
                        moduleConfig.AddInterceptor<test>();
                    });
        }
    }

    public class TestGrpcClientModule<TClient> : GrpcClientModule<TClient, TestGrpcClientResolver<TClient>>
        where TClient : ClientBase<TClient>
    {
        public TestGrpcClientModule(GrpcClientModuleConfig config, Application application) : base(
            config, application)
        {
        }
    }

    public class TestGrpcClientResolver<TClient> : IGrpcServiceAddressResolver<TClient>
        where TClient : ClientBase<TClient>
    {
        private int _port = 1000;

        public Task InitAsync()
        {
            return Task.CompletedTask;
        }

        public void SetPort(int port)
        {
            _port = port;
        }

        public Uri GetAddress()
        {
            return new Uri($"http://localhost:{_port}");
        }
    }
}
