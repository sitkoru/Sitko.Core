using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server.Tests
{
    public class TestApplication : WebApplication<TestStartup>
    {
        public TestApplication(string[] args) : base(args) =>
            this.AddGrpcServer(moduleOptions =>
            {
                moduleOptions.RegisterService<GrpcTestService>();
            });

        protected override void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
            base.ConfigureWebHostDefaults(webHostBuilder);
            webHostBuilder.UseTestServer();
        }
    }
}
