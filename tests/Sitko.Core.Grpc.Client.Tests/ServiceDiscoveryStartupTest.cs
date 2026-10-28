using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.ServiceDiscovery.Consul;
using Sitko.Core.ServiceDiscovery;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class ServiceDiscoveryStartupTest
    {
        [Fact]
        public async Task MissingAddressWithDisabledDiscoveryFailsAtStartup()
        {
            var exception = await Record.ExceptionAsync(static () => StartClient(
                new Dictionary<string, string?> { ["ServiceDiscovery:Enabled"] = "false" }, true));
            _ = exception.Should().NotBeNull();
            _ = exception.ToString().Should().Contain("accounting.v1.Invoice").And.Contain("ServiceDiscovery.Enabled");
        }

        [Fact]
        public async Task MissingAddressWithoutProviderFailsAtStartup()
        {
            var exception = await Record.ExceptionAsync(static () => StartClient([], false));
            _ = exception.Should().NotBeNull();
            _ = exception.ToString().Should().Contain("accounting.v1.Invoice").And.Contain("provider");
        }

        [Fact]
        public async Task InvalidAddressFailsAtStartupWithoutDiscoveryFallback()
        {
            var exception = await Record.ExceptionAsync(static () => StartClient(
                new Dictionary<string, string?> { ["Grpc:Client:Contracts:accounting.v1.Invoice:Address"] = "relative-path" }, true));
            _ = exception.Should().NotBeNull();
            _ = exception.ToString().Should().Contain("accounting.v1.Invoice").And.Contain("Address");
        }

        [Fact]
        public async Task ZeroDnsTimeoutFailsAtStartup()
        {
            var exception = await Record.ExceptionAsync(static () => StartClient(
                new Dictionary<string, string?> { ["ServiceDiscovery:Consul:DnsTimeout"] = "00:00:00" }, true));
            _ = exception.Should().NotBeNull();
            _ = exception.ToString().Should().Contain("ServiceDiscovery:Consul").And.Contain("DnsTimeout");
        }

        [Fact]
        public async Task HeartbeatLongerThanTtlFailsAtStartup()
        {
            var exception = await Record.ExceptionAsync(static () => StartClient(
                new Dictionary<string, string?>
                {
                    ["ServiceDiscovery:Consul:HeartbeatInterval"] = "00:00:20",
                    ["ServiceDiscovery:Consul:Ttl"] = "00:00:10"
                }, true));
            _ = exception.Should().NotBeNull();
            _ = exception.ToString().Should().Contain("ServiceDiscovery:Consul").And.Contain("HeartbeatInterval").And.Contain("Ttl");
        }

        private static async Task StartClient(Dictionary<string, string?> config, bool addConsul)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            var app = builder.AddSitkoCore();
            if (addConsul)
            {
                _ = app.AddConsulServiceDiscovery();
            }
            else
            {
                _ = app.AddServiceDiscovery();
            }
            _ = app.AddGrpcClient<Accounting.Invoice.InvoiceClient>();
            using var host = builder.Build();
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
