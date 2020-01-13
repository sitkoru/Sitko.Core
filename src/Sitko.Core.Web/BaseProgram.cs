using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Web
{
    public static class BaseProgram
    {
        public static IHostBuilder CreateBasicHostBuilder<TStartup>(this Application application)
            where TStartup : BaseStartup =>
            application.GetHostBuilder().ConfigureAppConfiguration(builder =>
            {
                builder.AddUserSecrets<TStartup>();
                builder.AddEnvironmentVariables();
            }).ConfigureWebHost(builder => builder.UseStartup<TStartup>());
    }
}
