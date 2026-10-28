using AwesomeAssertions;
using Grpc.Core;
using Grpc.Reflection.V1Alpha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class ReflectionConfigurationTest
    {
        [Fact]
        public async Task ReflectionIsDisabledByDefaultAndEnabledAfterConfigurationRestart()
        {
            var token = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>();
            foreach (var reflectionEnabled in new[] { false, true })
            {
                if (reflectionEnabled)
                {
                    config["Grpc:Server:EnableReflection"] = "true";
                }
                var (server, port) = await ClientAddressSelectionTest.StartServer<RecoveryServer>(config, certificate, false, token);
                await using var serverLifetime = server;
                var builder = Host.CreateApplicationBuilder();
                _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Grpc:Client:Default:Address"] = $"https://127.0.0.1:{port}"
                });
                var app = builder.AddSitkoCore();
                _ = app.AddGrpcClient<Accounting.Invoice.InvoiceClient>(options =>
                    DnsTlsContractTest.ConfigureClient(options, certificate));
                _ = app.AddGrpcClient<ServerReflection.ServerReflectionClient>(options =>
                    DnsTlsContractTest.ConfigureClient(options, certificate));
                using var clientHost = builder.Build();
                await clientHost.StartAsync(token);
                try
                {
                    var client = clientHost.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                    var reply = await client.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token);
                    _ = reply.Value.Should().Be("server-ok");
                    var reflection = clientHost.Services.GetRequiredService<ServerReflection.ServerReflectionClient>();
                    using var call = reflection.ServerReflectionInfo(deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: token);
                    await call.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = "" }, token);
                    await call.RequestStream.CompleteAsync();
                    if (reflectionEnabled)
                    {
                        _ = (await call.ResponseStream.MoveNext(token)).Should().BeTrue();
                        _ = call.ResponseStream.Current.ListServicesResponse.Service.Select(static service => service.Name)
                            .Should().Contain("accounting.v1.Invoice");
                    }
                    else
                    {
                        var error = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseStream.MoveNext(token));
                        _ = error.StatusCode.Should().Be(StatusCode.Unimplemented);
                    }
                }
                finally
                {
                    await clientHost.StopAsync(token);
                    await server.StopAsync(token);
                }
            }
        }
    }
}
