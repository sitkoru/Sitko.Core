using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
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
                    .GetService<IGrpcServiceAddressResolver<TestService.TestServiceClient>>();
            resolver.SetPort(4000);
            var provider1 = scope.GetService<IGrpcClientProvider<TestService.TestServiceClient>>();
            var client1 = provider1.Instance;
            Assert.NotNull(client1);
            Assert.Throws<RpcException>(() => client1.Request(new TestRequest()));
            Assert.Equal(new Uri("http://localhost:4000"), provider1.CurrentAddress);
            resolver.SetPort(5000);
            var provider2 = scope.GetService<IGrpcClientProvider<TestService.TestServiceClient>>();
            var client2 = provider2.Instance;
            Assert.NotNull(client2);
            Assert.Throws<RpcException>(() => client2.Request(new TestRequest()));
            Assert.Equal(new Uri("http://localhost:5000"), provider2.CurrentAddress);
            Assert.Equal(provider1.CurrentAddress, provider2.CurrentAddress);
        }
    }

    public class TestInterceptor : Interceptor
    {
    }

    public class GrpcClientScope : BaseTestScope
    {
        protected override TestApplication ConfigureApplication(TestApplication application, string name)
        {
            base.ConfigureApplication(application, name)
                .AddModule<TestGrpcClientModule<TestService.TestServiceClient>,
                    TestGrpcClientModuleOptions<TestService.TestServiceClient>
                >(
                    moduleOptions =>
                    {
                        moduleOptions.EnableHttp2UnencryptedSupport = true;
                        moduleOptions.DisableCertificatesValidation = true;
                        moduleOptions.AddInterceptor<TestInterceptor>();
                    });
            return application;
        }
    }

    public class
        TestGrpcClientModule<TClient> : GrpcClientModule<TClient, TestGrpcClientResolver<TClient>,
            TestGrpcClientModuleOptions<TClient>>
        where TClient : ClientBase<TClient>
    {
        public override string OptionsKey => "Grpc:Client:Test";
    }

    public class TestGrpcClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
        where TClient : ClientBase<TClient>
    {
    }

    public class TestGrpcClientResolver<TClient> : IGrpcServiceAddressResolver<TClient>
        where TClient : ClientBase<TClient>
    {
        private int port = 1000;

        public Task InitAsync() => Task.CompletedTask;

        public void SetPort(int newPort)
        {
            port = newPort;
            OnChange?.Invoke(this, EventArgs.Empty);
        }

        public Uri GetAddress() => new($"http://localhost:{port}");

        public event EventHandler? OnChange;
    }
}
