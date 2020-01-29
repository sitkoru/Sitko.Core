using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health
{
    public class HealthModule:BaseApplicationModule
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHealthChecks();
        }
    }
    
    public static class HealthModuleExtensions
    {
        public static T AddHealth<T>(this T application) where T : Application<T>
        {
            return application.AddModule<HealthModule>();
        }
    }

    public enum HealthState
    {
        Healthy,
        Unhealthy,
        Degraded
    }
}
