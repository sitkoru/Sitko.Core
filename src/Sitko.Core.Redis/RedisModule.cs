using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Helpers;

namespace Sitko.Core.Redis
{
    public class RedisModule : BaseApplicationModule<RedisModuleOptions>
    {
        public override string OptionsKey => "Redis:Connection";

        public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
            RedisModuleOptions startupOptions)
        {
            base.ConfigureServices(applicationContext, services, startupOptions);
            RegisterConnection(services, startupOptions);
        }

        public static void EnsureConnection(IApplicationContext applicationContext, IServiceCollection services)
        {
            if (services.Any(static descriptor => descriptor.ServiceType == typeof(RedisConnection) && !descriptor.IsKeyedService))
            {
                return;
            }

            var options = OptionsHelper.GetOptions<RedisModuleOptions, RedisModuleOptionsValidator>(
                applicationContext, "Redis:Connection", validateOptions: true);
            RegisterConnection(services, options);
        }

        private static void RegisterConnection(IServiceCollection services, RedisModuleOptions options)
        {
            _ = services.AddSingleton(_ => new RedisConnection(options));
            _ = services.AddSingleton(static provider => provider.GetRequiredService<RedisConnection>().Database);
        }
    }
}
