using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Sitko.Core.App.Localization
{
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IOptions<JsonStringLocalizerOptions> _options;
        private readonly ILogger<JsonStringLocalizerFactory> _logger;
        private readonly CachingService _cache;

        public JsonStringLocalizerFactory(IOptions<JsonStringLocalizerOptions> options,
            ILogger<JsonStringLocalizerFactory> logger)
        {
            _options = options;
            _logger = logger;
            _cache = new CachingService();
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
            var resource = new LocalizationResource(resourceSource);
            return GetLocalizer(resource, cultureInfo);
        }

        private Dictionary<string, string> GetCompliedResourceData(LocalizationResource resource,
            CultureInfo cultureInfo)
        {
            return _cache.GetOrAdd(GetCompiledCacheKey(resource, cultureInfo), entry =>
            {
                ConfigureCacheEntry(entry);
                var data = new Dictionary<string, string>();
                FillData(data, resource, cultureInfo);
                FillData(data, resource, cultureInfo.Parent);
                FillData(data, resource, CultureInfo.InvariantCulture);
                FillDefaultData(data, cultureInfo);
                FillDefaultData(data, cultureInfo.Parent);
                FillDefaultData(data, CultureInfo.InvariantCulture);
                return data;
            });
        }

        private void FillDefaultData(Dictionary<string, string> data, CultureInfo cultureInfo)
        {
            foreach (var localizationResource in _options.Value.LocalizationResources)
            {
                FillData(data, localizationResource, cultureInfo);
            }
        }

        private void FillData(Dictionary<string, string> data, LocalizationResource resource, CultureInfo cultureInfo)
        {
            var resourceData = GetResourceData(resource, cultureInfo);
            foreach ((string key, string value) in resourceData)
            {
                if (!data.ContainsKey(key))
                {
                    data[key] = value;
                }
            }
        }

        private Dictionary<string, string> GetResourceData(LocalizationResource resource,
            CultureInfo cultureInfo)
        {
            return _cache.GetOrAdd(GetCacheKey(resource, cultureInfo), entry =>
            {
                ConfigureCacheEntry(entry);
                return LoadResourceData(resource, cultureInfo);
            });
        }

        private void ConfigureCacheEntry(ICacheEntry entry)
        {
            var expirationTime = DateTime.Now.Add(TimeSpan.FromMinutes(_options.Value.CacheTimeInMinutes));
            var expirationToken = new CancellationChangeToken(
                new CancellationTokenSource(TimeSpan.FromMinutes(_options.Value.CacheTimeInMinutes)
                    .Add(TimeSpan.FromSeconds(1))).Token);
            entry
                // Pin to cache.
                .SetPriority(CacheItemPriority.NeverRemove)
                // Set the actual expiration time
                .SetAbsoluteExpiration(expirationTime)
                // Force eviction to run
                .AddExpirationToken(expirationToken)
                // Add eviction callback
                .RegisterPostEvictionCallback(callback: CacheItemRemoved, state: this);
        }

        private void CacheItemRemoved(object key, object value, EvictionReason reason, object state)
        {
            _logger.LogDebug("Cache entry with key {Key} is expired. Reason: {Reason}", key, reason);
        }

        private Dictionary<string, string> LoadResourceData(LocalizationResource resource,
            CultureInfo cultureInfo)
        {
            Assembly satelliteAssembly;
            try
            {
                satelliteAssembly = !Equals(cultureInfo, CultureInfo.InvariantCulture)
                    ? resource.Assembly.GetSatelliteAssembly(cultureInfo)
                    : resource.Assembly;
            }
            catch (FileNotFoundException exception)
            {
                _logger.LogInformation(exception,
                    "Could not find satellite assembly for '{CultureInfoName}': {Message}",
                    cultureInfo.Name, exception.Message);
                return new Dictionary<string, string>();
            }

            var resourceFileName = $"{resource.Name}.json";
            var names = satelliteAssembly.GetManifestResourceNames();
            var name = names.FirstOrDefault(n => n.EndsWith(resourceFileName));
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogInformation(
                    "Resource '{ResourceName}' not found for '{CultureInfoName}'",
                    resource.Name, cultureInfo.Name);
                return new Dictionary<string, string>();
            }

            var stream = satelliteAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                _logger.LogInformation(
                    "Resource '{ResourceName}' not found for '{CultureInfoName}'",
                    resource.Name, cultureInfo.Name);
                return new Dictionary<string, string>();
            }

            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
            var resource = new LocalizationResource(baseName, Assembly.GetEntryAssembly()!);
            return GetLocalizer(resource, cultureInfo);
        }

        private IStringLocalizer GetLocalizer(LocalizationResource resource, CultureInfo cultureInfo)
        {
            var data = GetCompliedResourceData(resource, cultureInfo);
            return new JsonStringLocalizer(data);
        }

        private string GetCacheKey(LocalizationResource resource, CultureInfo cultureInfo)
        {
            return resource.Name + ';' + resource.Assembly.FullName + ';' + cultureInfo.Name;
        }

        private string GetCompiledCacheKey(LocalizationResource resource, CultureInfo cultureInfo)
        {
            return GetCacheKey(resource, cultureInfo) + "_compiled";
        }
    }

    public class LocalizationResource
    {
        public Assembly Assembly { get; }

        public string Name { get; }

        public LocalizationResource(Type resource)
        {
            TypeInfo resourceType = resource.GetTypeInfo();
            var typeName = resourceType.Name;
            if (resourceType.IsGenericType)
            {
                typeName = typeName.Remove(typeName.IndexOf('`'));
            }

            Name = typeName;
            Assembly = resourceType.Assembly;
        }

        public LocalizationResource(string name, Assembly assembly)
        {
            Name = name;
            Assembly = assembly;
        }
    };

    public class JsonStringLocalizerOptions
    {
        private readonly List<LocalizationResource> _localizationResources = new();
        public LocalizationResource[] LocalizationResources => _localizationResources.ToArray();

        public int CacheTimeInMinutes { get; set; } = 60;

        public JsonStringLocalizerOptions AddDefaultResource<T>()
        {
            return AddDefaultResource(typeof(T));
        }

        public JsonStringLocalizerOptions AddDefaultResource(Type resourceType)
        {
            _localizationResources.Add(new LocalizationResource(resourceType));
            return this;
        }
    }
}
