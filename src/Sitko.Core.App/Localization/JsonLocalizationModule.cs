using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitko.Core.App.Localization
{
    public class JsonLocalizationModule : LocalizationModule<JsonLocalizationModuleOptions, JsonStringLocalizerFactory>
    {
        public override string GetOptionsKey()
        {
            return "Localization:Json";
        }
    }

    public class JsonLocalizationModuleOptions : LocalizationModuleOptions
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
