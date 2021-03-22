using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server.Tests
{
    public class TestStartup : BaseStartup<TestApplication>
    {
        public TestStartup(IConfiguration configuration, IHostEnvironment environment) : base(
            configuration, environment)
        {
        }
    }
}