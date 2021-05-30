using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Blazor;

namespace Sitko.Core.Apps.Vault
{
    public class Startup : BlazorStartup
    {
        public Startup(IConfiguration configuration, IHostEnvironment environment) : base(configuration, environment)
        {
        }
    }
}
