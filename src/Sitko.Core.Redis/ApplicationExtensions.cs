using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Redis
{
    [PublicAPI]
    public static class ApplicationExtensions
    {
        public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder hostApplicationBuilder,
            Action<IApplicationContext, RedisModuleOptions> configure, string? optionsKey = null)
        {
            _ = hostApplicationBuilder.GetSitkoCore().AddRedis(configure, optionsKey);
            return hostApplicationBuilder;
        }

        public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder hostApplicationBuilder,
            Action<RedisModuleOptions>? configure = null, string? optionsKey = null)
        {
            _ = hostApplicationBuilder.GetSitkoCore().AddRedis(configure, optionsKey);
            return hostApplicationBuilder;
        }

        public static ISitkoCoreApplicationBuilder AddRedis(this ISitkoCoreApplicationBuilder applicationBuilder,
            Action<IApplicationContext, RedisModuleOptions> configure, string? optionsKey = null) =>
            applicationBuilder.AddModule<RedisModule, RedisModuleOptions>(configure, optionsKey);

        public static ISitkoCoreApplicationBuilder AddRedis(this ISitkoCoreApplicationBuilder applicationBuilder,
            Action<RedisModuleOptions>? configure = null, string? optionsKey = null) =>
            applicationBuilder.AddModule<RedisModule, RedisModuleOptions>(configure, optionsKey);
    }
}
