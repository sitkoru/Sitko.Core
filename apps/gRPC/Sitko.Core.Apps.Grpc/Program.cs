using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Sitko.Core.App.Web;
using Sitko.Core.Apps.Grpc;
using Sitko.Core.Apps.Grpc.Services;
using Sitko.Core.Grpc.Client;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery.Consul;
using FooService = Sitko.Core.Apps.Grpc.Services.FooService;

var builder = WebApplication.CreateBuilder(args);

using var certificate = CertificateLoader.LoadFromStoreCert("localhost", "My", StoreLocation.CurrentUser,
    allowInvalid: false);
_ = builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listen =>
    {
        if (listen.IPEndPoint is { } endpoint)
        {
            endpoint.Address = IPAddress.Loopback;
        }
    });
    // Application:Web генерирует сертификат; для демо используем доверенный сертификат разработки.
    options.ConfigureHttpsDefaults(https => https.OnAuthenticate = (_, ssl) =>
    {
        ssl.ServerCertificate = certificate;
        ssl.ServerCertificateContext = SslStreamCertificateContext.Create(certificate, null);
    });
});

var sitko = builder.AddSitkoCoreWeb().AddConsulServiceDiscovery();
_ = sitko.AddGrpcServer().RegisterService<GreeterService>().RegisterService<FooService>();
_ = sitko.AddGrpcClient<Greeter.GreeterClient>()
    .AddGrpcClient<Sitko.Core.Apps.Grpc.FooService.FooServiceClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
_ = app.MapSitkoCore();
_ = app.MapGet("/Test", static async context =>
{
    var client = context.RequestServices.GetRequiredService<Sitko.Core.Apps.Grpc.FooService.FooServiceClient>();
    var result = await client.FooAsync(new FooRequest { Bar = "123" });
    await context.Response.WriteAsync(result.Baz);
});
app.Run();
