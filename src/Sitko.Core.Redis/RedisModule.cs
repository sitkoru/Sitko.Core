using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Redis
{
    public class RedisModule : BaseApplicationModule<RedisModuleOptions>
    {
        public override string OptionsKey => "Redis";

        public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
            RedisModuleOptions startupOptions)
        {
            base.ConfigureServices(applicationContext, services, startupOptions);
            _ = services.AddSingleton(_ => new RedisConnection(startupOptions));
            _ = services.AddSingleton(static provider => provider.GetRequiredService<RedisConnection>().Database);
        }
    }
}
