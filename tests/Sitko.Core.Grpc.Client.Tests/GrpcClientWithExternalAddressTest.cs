using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Sitko.Core.Grpc.Client.Tests;

public class GrpcClientWithExternalAddressTest(ITestOutputHelper testOutputHelper)
    : GrpcClientTest<GrpcClientWithExternalAddressScope>(testOutputHelper);

public class GrpcClientWithExternalAddressScope : GrpcClientScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name)
    {
        base.ConfigureApplication(hostBuilder, name);
        hostBuilder.AddExternalGrpcClient<TestService.TestServiceClient>(options =>
        {
            options.Address = new Uri($"https://127.0.0.1:{HttpPort}");
            options.DisableCertificatesValidation = true;
        });
        return hostBuilder;
    }
}
