using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Sitko.Core.App.Localization
{
    public static class JsonLocalizationServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonLocalization(this IServiceCollection services,
            Action<JsonStringLocalizerOptions>? configure = null)
        {
            services.TryAddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
            services.Configure<JsonStringLocalizerOptions>(options =>
            {
                configure?.Invoke(options);
            });
            return services;
        }
    }
}
