using Sitko.Core.App.Web;
using Sitko.Core.Apps.Grpc;
using Sitko.Core.Apps.Grpc.Services;
using Sitko.Core.Grpc.Client;
using Sitko.Core.Grpc.Server;
using Sitko.Core.ServiceDiscovery.Consul;
using FooService = Sitko.Core.Apps.Grpc.Services.FooService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
_ = builder.AddSitkoCoreWeb();
_ = builder.AddGrpcServer().RegisterService<GreeterService>().RegisterService<FooService>();
_ = builder
    .AddGrpcClient<Greeter.GreeterClient>(static options => options.DisableCertificatesValidation = true)
    .AddGrpcClient<Sitko.Core.Apps.Grpc.FooService.FooServiceClient>(static options =>
        options.DisableCertificatesValidation = true);
_ = builder.AddSitkoCoreWeb().AddConsulServiceDiscovery();

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
