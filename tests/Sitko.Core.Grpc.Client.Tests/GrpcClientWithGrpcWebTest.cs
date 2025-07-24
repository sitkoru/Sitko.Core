using Microsoft.Extensions.Hosting;
using Sitko.Core.Grpc.Server;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests;

public class GrpcClientWithGrpcWebTest(ITestOutputHelper testOutputHelper)
    : GrpcClientTest<GrpcClientWithGrpcWebScope>(testOutputHelper);

public class GrpcClientWithGrpcWebScope : GrpcClientScope
{
    protected override void RegisterService(GrpcServerModuleOptions options) =>
        options.RegisterServiceWithGrpcWeb<TestServiceServer>();

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.AddGrpcWebClient<TestService.TestServiceClient>(options =>
        {
            options.Address = new Uri($"https://127.0.0.1:{HttpPort}");
            options.DisableCertificatesValidation = true;
        });
        return hostBuilder;
    }
}
