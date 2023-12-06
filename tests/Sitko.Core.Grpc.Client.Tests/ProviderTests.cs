using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.Discovery;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Grpc.Client.Tests;

public class ProviderTests : BaseTest<GrpcClientScope>
{
    public ProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TestDependencyInjection()
    {
        var scope = await GetScopeAsync();

        var resolver =
            (TestGrpcClientResolver<TestService.TestServiceClient>)scope
                .GetService<IGrpcServiceAddressResolver<TestService.TestServiceClient>>();
        resolver.SetPort(4000);
        var factory = scope.GetService<GrpcCallInvokerFactory>();
        var client1 = scope.GetService<TestService.TestServiceClient>();
        Assert.NotNull(client1);
        Assert.Throws<RpcException>(() => client1.Request(new TestRequest()));
        Assert.Equal(new Uri("http://localhost:4000"), factory.GetClientAddress<TestService.TestServiceClient>());
        resolver.SetPort(5000);
        var client2 = scope.GetService<TestService.TestServiceClient>();
        Assert.NotNull(client2);
        Assert.Throws<RpcException>(() => client2.Request(new TestRequest()));
        Assert.Equal(new Uri("http://localhost:5000"), factory.GetClientAddress<TestService.TestServiceClient>());
    }
}

public class TestInterceptor : Interceptor;

public class GrpcClientScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.GetSitkoCore()
            .AddModule<TestGrpcClientModule<TestService.TestServiceClient>,
                TestGrpcClientModuleOptions<TestService.TestServiceClient>>(
                moduleOptions =>
                {
                    moduleOptions.EnableHttp2UnencryptedSupport = true;
                    moduleOptions.DisableCertificatesValidation = true;
                    moduleOptions.AddInterceptor<TestInterceptor>();
                });
        return hostBuilder;
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
    where TClient : ClientBase<TClient>;

public class TestGrpcClientResolver<TClient> : IGrpcServiceAddressResolver<TClient>
    where TClient : ClientBase<TClient>
{
    private int port = 1000;

    public Task InitAsync() => Task.CompletedTask;

    public Uri GetAddress() => new($"http://localhost:{port}");

    public event EventHandler? OnChange;

    public void SetPort(int newPort)
    {
        port = newPort;
        OnChange?.Invoke(this, EventArgs.Empty);
    }
}
