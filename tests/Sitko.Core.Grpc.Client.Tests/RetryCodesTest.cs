using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Server;
using Xunit;

namespace Sitko.Core.Grpc.Client.Tests
{
    public class RetryCodesTest
    {
        [Theory]
        [InlineData(StatusCode.ResourceExhausted, false)]
        [InlineData(StatusCode.Unavailable, true)]
        public async Task ContractRetryCodesReplaceDefaultWithoutDisablingAllowedRetries(StatusCode firstStatus,
            bool expectSuccess)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var certificate = ClientAddressSelectionTest.CreateCertificate();
            var config = new Dictionary<string, string?>
            {
                ["Grpc:Client:Default:RetryPolicy:MaxAttempts"] = "2",
                ["Grpc:Client:Default:RetryPolicy:InitialBackoff"] = "00:00:00.010",
                ["Grpc:Client:Default:RetryPolicy:MaxBackoff"] = "00:00:00.010",
                ["Grpc:Client:Default:RetryPolicy:BackoffMultiplier"] = "1",
                ["Grpc:Client:Default:RetryPolicy:RetryableStatusCodes:0"] = "ResourceExhausted",
                ["Grpc:Client:Default:RetryPolicy:RetryableStatusCodes:1"] = "Unavailable",
                ["Grpc:Client:Contracts:options.v1.Invoice:RetryPolicy:RetryableStatusCodes:0"] = "Unavailable"
            };
            var service = new RetryStatusServer(firstStatus);
            var (server, port) = await ClientAddressSelectionTest.StartServer<RetryStatusServer>(config,
                certificate, false, cancellationToken, app =>
                {
                    _ = app.ConfigureServices(services => _ = services.AddSingleton(service));
                    _ = app.AddGrpcServer().RegisterService<RetryStatusServer>();
                });
            await using var serverLifetime = server;
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(config);
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Grpc:Client:Default:Address"] = $"https://127.0.0.1:{port}"
            });
            _ = builder.AddSitkoCore().AddGrpcClient<Payload.Invoice.InvoiceClient>(options =>
                DnsTlsContractTest.ConfigureClient(options, certificate));
            using var client = builder.Build();
            await client.StartAsync(cancellationToken);
            try
            {
                var invoice = client.Services.GetRequiredService<Payload.Invoice.InvoiceClient>();
                if (expectSuccess)
                {
                    var reply = await invoice.TransferAsync(new Payload.TransferRequest(),
                        deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken);
                    Assert.Equal("retry-ok", reply.Confirmation);
                }
                else
                {
                    var error = await Assert.ThrowsAsync<RpcException>(async () =>
                        await invoice.TransferAsync(new Payload.TransferRequest(),
                            deadline: DateTime.UtcNow.AddSeconds(15), cancellationToken: cancellationToken));
                    Assert.Equal(StatusCode.ResourceExhausted, error.StatusCode);
                }
            }
            finally
            {
                await client.StopAsync(cancellationToken);
                await server.StopAsync(cancellationToken);
            }
        }
    }

    public class RetryStatusServer(StatusCode firstStatus) : Payload.Invoice.InvoiceBase
    {
        private int calls;

        public override Task<Payload.TransferReply> Transfer(Payload.TransferRequest request, ServerCallContext context)
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                throw new RpcException(new Status(firstStatus, "first-attempt"));
            }
            return Task.FromResult(new Payload.TransferReply { Confirmation = "retry-ok" });
        }
    }
}
