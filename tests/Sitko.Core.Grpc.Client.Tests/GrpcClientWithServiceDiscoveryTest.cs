using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Consul;
using Sitko.Core.ServiceDiscovery;
using Sitko.Core.ServiceDiscovery.Resolver.Consul;
using Sitko.Core.ServiceDiscovery.Server.Consul;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests;

public class GrpcClientWithServiceDiscoveryTest(ITestOutputHelper testOutputHelper)
    : GrpcClientTest<GrpcClientWithServiceDiscoveryScope>(testOutputHelper)
{
    protected override async Task AfterRequestStartAsync(GrpcClientWithServiceDiscoveryScope scope) =>
        await scope.StartWebApplicationAsync();
}

public class GrpcClientWithServiceDiscoveryScope : GrpcClientScope
{
    protected override bool RunApplication => false;
    protected override bool EnableServiceDiscovery => true;

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);

        hostBuilder.AddGrpcClient<TestService.TestServiceClient>(options =>
            options.DisableCertificatesValidation = true);

        hostBuilder
            .AddConsul()
            .AddServiceDiscovery<ConsulServiceDiscoveryRegistrar, ConsulServiceDiscoveryResolver>();
        return hostBuilder;
    }

    protected override WebApplicationBuilder ConfigureWebApplication(WebApplicationBuilder webApplicationBuilder,
        string name)
    {
        base.ConfigureWebApplication(webApplicationBuilder, name);

        webApplicationBuilder
            .AddConsul()
            .AddServiceDiscovery<ConsulServiceDiscoveryRegistrar, ConsulServiceDiscoveryResolver>();

        return webApplicationBuilder;
    }
}
