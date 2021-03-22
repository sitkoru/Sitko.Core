using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server.Tests
{
    public class TestApplication : WebApplication<TestApplication>
    {
        public TestApplication(string[] args) : base(args)
        {
            AddModule<GrpcServerModule, GrpcServerOptions>((_, _, moduleConfig) =>
            {
                moduleConfig.RegisterService<TestServiceImpl>();
            }).UseStartup<TestStartup>();
        }
    }
}