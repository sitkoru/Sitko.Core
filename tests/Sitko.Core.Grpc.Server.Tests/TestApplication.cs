using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server.Tests
{
    public class TestApplication : WebApplication<TestStartup>
    {
        public TestApplication(string[] args) : base(args)
        {
            AddModule<GrpcServerModule, GrpcServerOptions>((_, _, moduleConfig) =>
            {
                moduleConfig.RegisterService<TestServiceImpl>();
            });
        }

        protected override void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
            base.ConfigureWebHostDefaults(webHostBuilder);
            webHostBuilder.UseTestServer();
        }
    }
}
