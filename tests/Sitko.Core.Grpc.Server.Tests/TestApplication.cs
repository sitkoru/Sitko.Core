using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server.Tests;

public class TestApplication : WebApplication<TestStartup>
{
    public TestApplication(string[] args) : base(args) =>
        this.AddGrpcServer(moduleOptions =>
        {
            moduleOptions.RegisterService<GrpcTestService>();
        });

    protected override void ConfigureWebHostDefaults(IApplicationContext applicationContext,
        IWebHostBuilder webHostBuilder)
    {
        base.ConfigureWebHostDefaults(applicationContext, webHostBuilder);
        webHostBuilder.UseTestServer();
    }
}
