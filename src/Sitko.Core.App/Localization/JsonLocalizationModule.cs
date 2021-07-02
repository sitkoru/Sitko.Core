using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Sitko.Core.App.Localization
{
    public class JsonLocalizationModule : BaseApplicationModule<JsonLocalizationModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return "Localization:Json";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            JsonLocalizationModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.TryAddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        }
    }

    public class JsonLocalizationModuleOptions : BaseModuleOptions
    {
        private readonly HashSet<Type> _defaultResources = new();
        public Type[] DefaultResources => _defaultResources.ToArray();

        public int CacheTimeInMinutes { get; set; } = 60;

        public JsonLocalizationModuleOptions AddDefaultResource<T>()
        {
            return AddDefaultResource(typeof(T));
        }

        public JsonLocalizationModuleOptions AddDefaultResource(Type resourceType)
        {
            _defaultResources.Add(resourceType);
            return this;
        }
    }
}
