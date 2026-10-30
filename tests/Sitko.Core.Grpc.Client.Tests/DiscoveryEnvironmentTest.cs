using AwesomeAssertions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class DiscoveryEnvironmentTest
    {
        [Fact]
        public async Task SameContractStaysInItsEnvironmentAfterOtherEnvironmentStops()
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = DiscoveryLifecycleTest.CreateConsul();
            await consul.StartAsync(token);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var configA = DiscoveryLifecycleTest.Configuration(consul);
            configA["Application:Environment"] = "sd-test-a";
            var configB = new Dictionary<string, string?>(configA) { ["Application:Environment"] = "sd-test-b" };
            var (serverA, _) = await ClientAddressSelectionTest.StartServer<EnvironmentAServer>(configA, certificate, true, token);
            await using var serverALifetime = serverA;
            var (serverB, _) = await ClientAddressSelectionTest.StartServer<EnvironmentBServer>(configB, certificate, true, token);
            await using var serverBLifetime = serverB;
            using var hostA = DiscoveryLifecycleTest.CreateClient(configA, certificate);
            using var hostB = DiscoveryLifecycleTest.CreateClient(configB, certificate);
            await hostA.StartAsync(token);
            await hostB.StartAsync(token);
            try
            {
                var clientA = hostA.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                var clientB = hostB.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                await DiscoveryLifecycleTest.WaitForReply(clientA, "env-a-ok", token);
                await DiscoveryLifecycleTest.WaitForReply(clientB, "env-b-ok", token);
                for (var i = 0; i < 5; i++)
                {
                    var replyA = await clientA.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token);
                    var replyB = await clientB.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token);
                    _ = replyA.Value.Should().Be("env-a-ok");
                    _ = replyB.Value.Should().Be("env-b-ok");
                }
                await serverA.StopAsync(token);
                for (var i = 0; i < 5; i++)
                {
                    _ = await Assert.ThrowsAsync<RpcException>(async () =>
                        await clientA.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token));
                    var replyB = await clientB.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token);
                    _ = replyB.Value.Should().Be("env-b-ok");
                    await Task.Delay(300, token);
                }
            }
            finally
            {
                await hostA.StopAsync(token);
                await hostB.StopAsync(token);
                await serverA.StopAsync(token);
                await serverB.StopAsync(token);
            }
        }
    }

    public class EnvironmentAServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context) =>
            Task.FromResult(new Accounting.Reply { Value = "env-a-ok" });
    }

    public class EnvironmentBServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context) =>
            Task.FromResult(new Accounting.Reply { Value = "env-b-ok" });
    }
}
