using NewBlazor;
using NewBlazor.Components;
using Sitko.Core.App.Web;
using Sitko.Core.Grpc.Client;
using Sitko.Core.Grpc.Server;
using Sitko.Core.Grpc.Server.Tests;
using Sitko.Core.Pdf;
using Sitko.Core.Puppeteer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddSitkoCore(args)
    .AddModule<PuppeteerModule, PuppeteerModuleOptions>()
    .AddModule<PdfRendererModule>()
    .AddGrpcServer(options =>
    {
        options.RegisterService<TestServiceImp>();
    })
    .AddExternalGrpcClient<TestService.TestServiceClient>(options =>
    {
        options.Address = new Uri("https://localhost:7023/");
        options.DisableCertificatesValidation = true;
    });


var app = builder.Build();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapSitkoCore();
app.UseAntiforgery();
app.Run();
