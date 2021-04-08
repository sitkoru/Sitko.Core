using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.Discovery;
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
                (TestGrpcClientResolver<TestService.TestServiceClient>)scope
                    .Get<IGrpcServiceAddressResolver<TestService.TestServiceClient>>();
            resolver.SetPort(4000);
            var provider1 = scope.Get<IGrpcClientProvider<TestService.TestServiceClient>>();
            var client1 = provider1.Instance;
            Assert.NotNull(client1);
            Assert.Throws<RpcException>(() => client1.Request(new TestRequest()));
            Assert.Equal(new Uri("http://localhost:4000"), provider1.CurrentAddress);
            resolver.SetPort(5000);
            var provider2 = scope.Get<IGrpcClientProvider<TestService.TestServiceClient>>();
            var client2 = provider2.Instance;
            Assert.NotNull(client2);
            Assert.Throws<RpcException>(() => client2.Request(new TestRequest()));
            Assert.Equal(new Uri("http://localhost:5000"), provider2.CurrentAddress);
            Assert.Equal(provider1.CurrentAddress, provider2.CurrentAddress);
        }
    }

    public class test : Interceptor
    {
    }

    public class GrpcClientScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            return base.ConfigureApplication(application, name)
                .AddModule<TestApplication, TestGrpcClientModule<TestService.TestServiceClient>,
                    TestGrpcClientModuleConfig
                >(
                    (_, _, moduleConfig) =>
                    {
                        moduleConfig.EnableHttp2UnencryptedSupport = true;
                        moduleConfig.DisableCertificatesValidation = true;
                        moduleConfig.AddInterceptor<test>();
                    });
        }
    }

    public class
        TestGrpcClientModule<TClient> : GrpcClientModule<TClient, TestGrpcClientResolver<TClient>,
            TestGrpcClientModuleConfig>
        where TClient : ClientBase<TClient>
    {
        public TestGrpcClientModule(TestGrpcClientModuleConfig config, Application application) : base(
            config, application)
        {
        }
    }

    public class TestGrpcClientModuleConfig : GrpcClientModuleConfig
    {
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
            OnChange?.Invoke(this, EventArgs.Empty);
        }

        public Uri GetAddress()
        {
            return new($"http://localhost:{_port}");
        }

        public event EventHandler? OnChange;
    }
}
