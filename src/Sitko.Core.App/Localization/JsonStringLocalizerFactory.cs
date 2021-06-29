using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Localization
{
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly ConcurrentDictionary<string, IStringLocalizer> _cache = new();
        private readonly ILoggerFactory _loggerFactory;

        public JsonStringLocalizerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            TypeInfo resourceType = resourceSource.GetTypeInfo();
            CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
            string resourceName = $"{resourceType.Name}.json";
            return GetCachedLocalizer(resourceName, resourceType.Assembly, cultureInfo);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
            string resourceName = $"{baseName}.json";
            return GetCachedLocalizer(resourceName, Assembly.GetEntryAssembly()!, cultureInfo);
        }

        private IStringLocalizer GetCachedLocalizer(string resourceName, Assembly assembly, CultureInfo cultureInfo)
        {
            string cacheKey = GetCacheKey(resourceName, assembly, cultureInfo);
            return _cache.GetOrAdd(cacheKey,
                new JsonStringLocalizer(resourceName, assembly, cultureInfo,
                    _loggerFactory.CreateLogger<JsonStringLocalizer>()));
        }

        private string GetCacheKey(string resourceName, Assembly assembly, CultureInfo cultureInfo)
        {
            return resourceName + ';' + assembly.FullName + ';' + cultureInfo.Name;
        }
    }
}