using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Server;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class GrpcOptionsTest
    {
        private const int MiB = 1024 * 1024;

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ContractClientReceiveLimitOverridesDefaultForTwoMiBResponse(bool compressed)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Grpc:Server:Default:MaxSendMessageSize"] = "16777216",
                ["Grpc:Client:Default:MaxReceiveMessageSize"] = "1048576",
                ["Grpc:Client:Contracts:options.v1.Reports:MaxReceiveMessageSize"] = "4194304"
            };
            var (server, port) = await ClientAddressSelectionTest.StartServer<LimitInvoiceServer>(config,
                certificate, false, cancellationToken, app =>
                    app.AddGrpcServer(options => options.ConfigureGrpcService = service =>
                            service.ResponseCompressionAlgorithm = compressed ? "gzip" : null)
                        .RegisterService<LimitInvoiceServer>().RegisterService<LimitReportsServer>());
            await using var serverLifetime = server;
            using var client = await StartClients(config, certificate, port, cancellationToken);
            try
            {
                var invoice = client.Services.GetRequiredService<Payload.Invoice.InvoiceClient>();
                var reports = client.Services.GetRequiredService<Payload.Reports.ReportsClient>();
                var error = await Assert.ThrowsAsync<RpcException>(async () =>
                    await invoice.TransferAsync(new Payload.TransferRequest { ResponseSize = 2 * MiB },
                        deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken));
                Assert.Equal(StatusCode.ResourceExhausted, error.StatusCode);
                var reply = await reports.TransferAsync(new Payload.TransferRequest { ResponseSize = 2 * MiB },
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken);
                Assert.Equal("reports-ok", reply.Confirmation);
                Assert.Equal(2 * MiB, reply.Data.Length);
                Assert.True(reply.Data.Span.SequenceEqual(Enumerable.Repeat((byte)0x5a, 2 * MiB).ToArray()));
            }
            finally
            {
                await client.StopAsync(cancellationToken);
                await server.StopAsync(cancellationToken);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ContractServerReceiveLimitOverridesDefaultForTwoMiBRequest(bool compressed)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Grpc:Server:Default:MaxReceiveMessageSize"] = "1048576",
                ["Grpc:Server:Contracts:options.v1.Reports:MaxReceiveMessageSize"] = "4194304",
                ["Grpc:Client:Default:MaxSendMessageSize"] = "16777216"
            };
            var (server, port) = await ClientAddressSelectionTest.StartServer<LimitInvoiceServer>(config,
                certificate, false, cancellationToken, static app =>
                    app.AddGrpcServer().RegisterService<LimitInvoiceServer>().RegisterService<LimitReportsServer>());
            await using var serverLifetime = server;
            using var client = await StartClients(config, certificate, port, cancellationToken);
            try
            {
                var request = new Payload.TransferRequest
                {
                    Data = ByteString.CopyFrom([.. Enumerable.Repeat((byte)0x5a, 2 * MiB)])
                };
                var headers = compressed ? new Metadata { { "grpc-internal-encoding-request", "gzip" } } : null;
                var invoice = client.Services.GetRequiredService<Payload.Invoice.InvoiceClient>();
                var reports = client.Services.GetRequiredService<Payload.Reports.ReportsClient>();
                var error = await Assert.ThrowsAsync<RpcException>(async () =>
                    await invoice.TransferAsync(request, headers, deadline: DateTime.UtcNow.AddSeconds(15),
                        cancellationToken: cancellationToken));
                Assert.Equal(StatusCode.ResourceExhausted, error.StatusCode);
                var reply = await reports.TransferAsync(request, headers, deadline: DateTime.UtcNow.AddSeconds(15),
                    cancellationToken: cancellationToken);
                Assert.Equal("reports-ok", reply.Confirmation);
            }
            finally
            {
                await client.StopAsync(cancellationToken);
                await server.StopAsync(cancellationToken);
            }
        }

        [Theory]
        [InlineData(4, 6291456, 9437184)]
        [InlineData(3, 3145728, 6291456)]
        [InlineData(2, 1572864, 3145728)]
        [InlineData(1, 524288, 1572864)]
        public async Task ServerReceiveLimitUsesConfigurationAndCallbacksInPriorityOrder(int levels,
            int acceptedSize, int rejectedSize)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Grpc:Server:Default:MaxReceiveMessageSize"] = "1048576",
                ["Grpc:Client:Default:MaxSendMessageSize"] = "16777216"
            };
            if (levels >= 3)
            {
                config["Grpc:Server:Contracts:options.v1.Invoice:MaxReceiveMessageSize"] = "4194304";
            }
            var (server, port) = await ClientAddressSelectionTest.StartServer<LimitInvoiceServer>(config,
                certificate, false, cancellationToken, app =>
                {
                    Action<GrpcServiceOptions>? specific = levels >= 4
                        ? static options => options.MaxReceiveMessageSize = 8 * MiB
                        : null;
                    _ = app.AddGrpcServer(options =>
                    {
                        if (levels >= 2)
                        {
                            options.ConfigureGrpcService = static service => service.MaxReceiveMessageSize = 2 * MiB;
                        }
                    }).RegisterService<LimitInvoiceServer>(specific);
                });
            await using var serverLifetime = server;
            using var client = await StartClients(config, certificate, port, cancellationToken);
            try
            {
                var invoice = client.Services.GetRequiredService<Payload.Invoice.InvoiceClient>();
                var reply = await invoice.TransferAsync(new Payload.TransferRequest
                {
                    Data = ByteString.CopyFrom([.. Enumerable.Repeat((byte)0x5a, acceptedSize)])
                }, deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken);
                Assert.Equal("invoice-ok", reply.Confirmation);
                var error = await Assert.ThrowsAsync<RpcException>(async () =>
                    await invoice.TransferAsync(new Payload.TransferRequest
                    {
                        Data = ByteString.CopyFrom([.. Enumerable.Repeat((byte)0x5a, rejectedSize)])
                    }, deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken));
                Assert.Equal(StatusCode.ResourceExhausted, error.StatusCode);
            }
            finally
            {
                await client.StopAsync(cancellationToken);
                await server.StopAsync(cancellationToken);
            }
        }

        [Theory]
        [InlineData(4, 6291456, 9437184)]
        [InlineData(3, 3145728, 6291456)]
        [InlineData(2, 1572864, 3145728)]
        [InlineData(1, 524288, 1572864)]
        public async Task ClientReceiveLimitUsesConfigurationAndCallbacksInPriorityOrder(int levels,
            int acceptedSize, int rejectedSize)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Grpc:Client:Default:MaxReceiveMessageSize"] = "1048576",
                ["Grpc:Server:Default:MaxSendMessageSize"] = "16777216"
            };
            if (levels >= 3)
            {
                config["Grpc:Client:Contracts:options.v1.Invoice:MaxReceiveMessageSize"] = "4194304";
            }
            var (server, port) = await ClientAddressSelectionTest.StartServer<LimitInvoiceServer>(config,
                certificate, false, cancellationToken);
            await using var serverLifetime = server;
            using var client = await StartClients(config, certificate, port, cancellationToken,
                app =>
                {
                    if (levels >= 2)
                    {
                        _ = app.ConfigureGrpcClients(static options => options.MaxReceiveMessageSize = 2 * MiB);
                    }
                }, options =>
                {
                    if (levels >= 4)
                    {
                        options.MaxReceiveMessageSize = 8 * MiB;
                    }
                });
            try
            {
                var invoice = client.Services.GetRequiredService<Payload.Invoice.InvoiceClient>();
                var reply = await invoice.TransferAsync(new Payload.TransferRequest { ResponseSize = acceptedSize },
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken);
                Assert.Equal("invoice-ok", reply.Confirmation);
                Assert.Equal(acceptedSize, reply.Data.Length);
                Assert.True(reply.Data.Span.SequenceEqual(Enumerable.Repeat((byte)0x5a, acceptedSize).ToArray()));
                var error = await Assert.ThrowsAsync<RpcException>(async () =>
                    await invoice.TransferAsync(new Payload.TransferRequest { ResponseSize = rejectedSize },
                        deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken));
                Assert.Equal(StatusCode.ResourceExhausted, error.StatusCode);
            }
            finally
            {
                await client.StopAsync(cancellationToken);
                await server.StopAsync(cancellationToken);
            }
        }

        [Fact]
        public async Task RepeatedServerRegistrationsUseFinalPolicyAndRemainApplicationLocal()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Grpc:Client:Default:MaxSendMessageSize"] = "16777216",
                ["Grpc:Server:Default:MaxReceiveMessageSize"] = "1048576"
            };
            var (serverA, portA) = await ClientAddressSelectionTest.StartServer<LimitInvoiceServer>(config,
                certificate, false, cancellationToken, static app =>
                {
                    RegisterInvoiceFeature(app);
                    RegisterReportsFeature(app);
                    _ = app.AddGrpcServer(static options =>
                        options.ConfigureGrpcService = static service => service.MaxReceiveMessageSize = 4 * MiB);
                    _ = app.AddGrpcServer(static options =>
                        options.ConfigureGrpcService = static service => service.MaxSendMessageSize = 16 * MiB);
                    var error = Assert.Throws<InvalidOperationException>(() =>
                        app.AddGrpcServer().RegisterService<AppBInvoiceServer>());
                    Assert.Contains("options.v1.Invoice", error.Message, StringComparison.Ordinal);
                });
            await using var serverALifetime = serverA;
            var (serverB, portB) = await ClientAddressSelectionTest.StartServer<AppBInvoiceServer>(config,
                certificate, false, cancellationToken);
            await using var serverBLifetime = serverB;
            using var clientA = await StartClients(config, certificate, portA, cancellationToken);
            using var clientB = await StartClients(config, certificate, portB, cancellationToken);
            try
            {
                var invoice = clientA.Services.GetRequiredService<Payload.Invoice.InvoiceClient>();
                var reports = clientA.Services.GetRequiredService<Payload.Reports.ReportsClient>();
                var request = new Payload.TransferRequest
                {
                    Data = ByteString.CopyFrom([.. Enumerable.Repeat((byte)0x5a, 2 * MiB)])
                };
                var invoiceReply = await invoice.TransferAsync(request, deadline: DateTime.UtcNow.AddSeconds(15),
                    cancellationToken: cancellationToken);
                var reportsReply = await reports.TransferAsync(request, deadline: DateTime.UtcNow.AddSeconds(15),
                    cancellationToken: cancellationToken);
                Assert.Equal("invoice-ok", invoiceReply.Confirmation);
                Assert.Equal("reports-ok", reportsReply.Confirmation);
                var secondApp = clientB.Services.GetRequiredService<Payload.Invoice.InvoiceClient>();
                var secondReply = await secondApp.TransferAsync(new Payload.TransferRequest(),
                    deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken);
                Assert.Equal("app-b-ok", secondReply.Confirmation);
            }
            finally
            {
                await clientA.StopAsync(cancellationToken);
                await clientB.StopAsync(cancellationToken);
                await serverA.StopAsync(cancellationToken);
                await serverB.StopAsync(cancellationToken);
            }
        }

        private static void RegisterInvoiceFeature(ISitkoCoreApplicationBuilder app) =>
            app.AddModule<InvoiceFeatureModule>().AddGrpcServer(static options =>
                    options.ConfigureGrpcService = static service => service.MaxReceiveMessageSize = MiB)
                .RegisterService<LimitInvoiceServer>();

        private static void RegisterReportsFeature(ISitkoCoreApplicationBuilder app) =>
            app.AddModule<ReportsFeatureModule>().AddGrpcServer().RegisterService<LimitInvoiceServer>()
                .RegisterService<LimitReportsServer>();

        private static async Task<IHost> StartClients(Dictionary<string, string?> config, X509Certificate2 certificate,
            int port, CancellationToken cancellationToken, Action<ISitkoCoreApplicationBuilder>? configure = null,
            Action<GrpcClientModuleOptions<Payload.Invoice.InvoiceClient>>? configureInvoice = null)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Grpc:Client:Default:Address"] = $"https://127.0.0.1:{port}"
            });
            var app = builder.AddSitkoCore();
            configure?.Invoke(app);
            _ = app.AddGrpcClient<Payload.Invoice.InvoiceClient>(options =>
            {
                ConfigureClient(options, certificate);
                configureInvoice?.Invoke(options);
            });
            _ = app.AddGrpcClient<Payload.Reports.ReportsClient>(options => ConfigureClient(options, certificate));
            var host = builder.Build();
            await host.StartAsync(cancellationToken);
            return host;
        }

        private static void ConfigureClient<TClient>(GrpcClientModuleOptions<TClient> options,
            X509Certificate2 certificate) where TClient : ClientBase<TClient>
        {
            DnsTlsContractTest.ConfigureClient(options, certificate);
            options.RetryPolicy = null;
        }
    }

    public class InvoiceFeatureModule : BaseApplicationModule
    {
        public override string OptionsKey => "InvoiceFeature";
    }

    public class ReportsFeatureModule : BaseApplicationModule
    {
        public override string OptionsKey => "ReportsFeature";
    }

    public class AppBInvoiceServer : Payload.Invoice.InvoiceBase
    {
        public override Task<Payload.TransferReply> Transfer(Payload.TransferRequest request, ServerCallContext context) =>
            Task.FromResult(PayloadReply.Create(request, "app-b-ok"));
    }

    public class LimitInvoiceServer : Payload.Invoice.InvoiceBase
    {
        public override Task<Payload.TransferReply> Transfer(Payload.TransferRequest request, ServerCallContext context) =>
            Task.FromResult(PayloadReply.Create(request, "invoice-ok"));
    }

    public class LimitReportsServer : Payload.Reports.ReportsBase
    {
        public override Task<Payload.TransferReply> Transfer(Payload.TransferRequest request, ServerCallContext context) =>
            Task.FromResult(PayloadReply.Create(request, "reports-ok"));
    }

    internal static class PayloadReply
    {
        internal static Payload.TransferReply Create(Payload.TransferRequest request, string confirmation)
        {
            Assert.True(request.Data.Span.IndexOfAnyExcept((byte)0x5a) < 0);
            var data = new byte[request.ResponseSize];
            Array.Fill(data, (byte)0x5a);
            return new Payload.TransferReply { Data = ByteString.CopyFrom(data), Confirmation = confirmation };
        }
    }
}
