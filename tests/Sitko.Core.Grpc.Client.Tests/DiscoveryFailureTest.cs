using System.Diagnostics;
using AwesomeAssertions;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class DiscoveryFailureTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SameApplicationsRecoverWhenConsulReturns(bool unavailableAtStartup)
        {
            var token = TestContext.Current.CancellationToken;
            await using var consul = DiscoveryLifecycleTest.CreateConsul();
            await consul.StartAsync(token);
            var config = DiscoveryLifecycleTest.Configuration(consul);
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var paused = unavailableAtStartup;
            if (unavailableAtStartup)
            {
                await consul.PauseAsync(token);
            }
            var (server, _) = await ClientAddressSelectionTest.StartServer<RecoveryServer>(
                config, certificate, true, token);
            await using var serverLifetime = server;
            using var clientHost = DiscoveryLifecycleTest.CreateClient(config, certificate);
            await clientHost.StartAsync(token);
            try
            {
                var client = clientHost.Services.GetRequiredService<Accounting.Invoice.InvoiceClient>();
                if (!unavailableAtStartup)
                {
                    await DiscoveryLifecycleTest.WaitForReply(client, "server-ok", token);
                    await consul.PauseAsync(token);
                    paused = true;
                }
                await WaitForUnavailable(client, token);
                await consul.UnpauseAsync(token);
                paused = false;
                _ = DiscoveryLifecycleTest.Configuration(consul)["ServiceDiscovery:Consul:Address"].Should()
                    .Be(config["ServiceDiscovery:Consul:Address"], "recovery must use the same Consul endpoint");
                _ = DiscoveryLifecycleTest.Configuration(consul)["ServiceDiscovery:Consul:DnsServers:0"].Should()
                    .Be(config["ServiceDiscovery:Consul:DnsServers:0"], "recovery must use the same DNS endpoint");
                await DiscoveryLifecycleTest.WaitForReply(client, "server-ok", token);
            }
            finally
            {
                if (paused)
                {
                    await consul.UnpauseAsync(token);
                }
                await clientHost.StopAsync(token);
                await server.StopAsync(token);
            }
        }

        private static async Task WaitForUnavailable(Accounting.Invoice.InvoiceClient client, CancellationToken token)
        {
            var transition = Stopwatch.StartNew();
            while (transition.Elapsed < TimeSpan.FromSeconds(58))
            {
                var call = Stopwatch.StartNew();
                try
                {
                    _ = await client.GetAsync(new Accounting.Request(), deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: token);
                }
                catch (RpcException) when (!token.IsCancellationRequested)
                {
                    _ = call.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
                    _ = transition.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
                    TestContext.Current.TestOutputHelper?.WriteLine($"Observed unavailable DNS after {transition.Elapsed}; failed RPC took {call.Elapsed}");
                    return;
                }
                await Task.Delay(100, token);
            }
            Assert.Fail("DNS must eventually discard the unavailable Consul endpoints");
        }
    }

    public class RecoveryServer : Accounting.Invoice.InvoiceBase
    {
        public override Task<Accounting.Reply> Get(Accounting.Request request, ServerCallContext context) =>
            Task.FromResult(new Accounting.Reply { Value = "server-ok" });
    }
}
