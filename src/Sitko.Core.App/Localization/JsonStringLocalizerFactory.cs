using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tempus;

namespace Sitko.Core.App.Localization
{
    public class JsonStringLocalizerFactory : IStringLocalizerFactory, IDisposable
    {
        private static ILogger<JsonStringLocalizerFactory> _logger = null!;
        private static Type[] s_defaultResources = new Type[0];
        private readonly IScheduledTask _clearCacheTask;

        private readonly Dictionary<int, Dictionary<string, string>> _data = new();

        private readonly Dictionary<int, JsonStringLocalizer> _instances = new();
        private readonly IDisposable _onChange;

        private TimeSpan _cacheTimout;
        private const char GenericSeparator = '`';

        public JsonStringLocalizerFactory(IOptionsMonitor<JsonLocalizationModuleOptions> options, IScheduler scheduler,
            ILogger<JsonStringLocalizerFactory> logger)
        {
            _logger = logger;
            ReadOptions(options.CurrentValue);
            _onChange = options.OnChange(ReadOptions);
            _clearCacheTask = scheduler.Schedule(_cacheTimout, _ => ClearCache(), (context, _) =>
            {
                _logger.LogError(context.Exception, "Error clear localization cache: {ErrorText}",
                    context.Exception.ToString());
                return Task.CompletedTask;
            });
        }


        public void Dispose()
        {
            _onChange.Dispose();
            _clearCacheTask.Cancel();
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            return GetLocalizer(resourceSource);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return GetLocalizer(null, baseName, Assembly.GetEntryAssembly()!);
        }

        private Task ClearCache()
        {
            _logger.LogDebug("Clear localization cache");
            _instances.Clear();
            _data.Clear();
            return Task.CompletedTask;
        }

        private void ReadOptions(JsonLocalizationModuleOptions localizerOptions)
        {
            _cacheTimout = TimeSpan.FromMinutes(localizerOptions.CacheTimeInMinutes);
            s_defaultResources = localizerOptions.DefaultResources;
        }

        private int GetCacheKey(string name, Assembly assembly, CultureInfo cultureInfo)
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + name.GetHashCode();
                hash = hash * 23 + assembly.GetHashCode();
                hash = hash * 23 + cultureInfo.GetHashCode();
                return hash;
            }
        }

        private JsonStringLocalizer GetLocalizer(Type? type, string? baseName = null, Assembly? baseAssembly = null)
        {
            var cultureInfo = CultureInfo.CurrentUICulture;
            var typeName = type?.Name ?? baseName!;
            if (type?.IsGenericType == true)
            {
                typeName = typeName.Remove(typeName.IndexOf(GenericSeparator));
            }

            var assembly = type?.Assembly ?? baseAssembly!;
            var cacheKey = GetCacheKey(typeName, assembly, cultureInfo);
            if (_instances.TryGetValue(cacheKey, out var instance))
            {
                return instance;
            }

            lock (_instances)
            {
                instance = new JsonStringLocalizer(GetResourceData(cacheKey, typeName, assembly, cultureInfo));
                _instances.TryAdd(cacheKey, instance);
            }

            return instance;
        }

        private Dictionary<string, string> GetResourceData(int cacheKey, string name, Assembly assembly,
            CultureInfo cultureInfo)
        {
            if (_data.TryGetValue(cacheKey, out var data))
            {
                return data;
            }

            lock (_data)
            {
                data = LoadResourcesData(name, assembly, cultureInfo);
                _data.TryAdd(cacheKey, data);
            }

            return data;
        }

        private static void FillData(Dictionary<string, string> data, Dictionary<string, string> resourceData)
        {
            foreach ((string key, string value) in resourceData)
            {
                if (!data.ContainsKey(key))
                {
                    data[key] = value;
                }
            }
        }

        private Dictionary<string, string> LoadResourcesData(string name, Assembly assembly,
            CultureInfo cultureInfo)
        {
            var data = new Dictionary<string, string>();
            var cultures = new[] { cultureInfo, cultureInfo.Parent, CultureInfo.InvariantCulture };
            bool loaded = LoadResourceData(data, name, assembly, cultures);

            foreach (var defaultResource in s_defaultResources)
            {
                var typeName = defaultResource.Name;
                if (defaultResource.IsGenericType)
                {
                    typeName = typeName.Remove(typeName.IndexOf(GenericSeparator));
                }

                if (!LoadResourceData(data, typeName, defaultResource.Assembly, cultures))
                {
                    loaded = false;
                }
            }

            if (!loaded)
            {
                _logger.LogWarning("No resources found for '{ResourceName}'", name);
            }

            return data;
        }

        private static bool LoadResourceData(Dictionary<string, string> data, string name, Assembly assembly,
            CultureInfo[] cultures)
        {
            var loaded = false;
            foreach (var cultureInfo in cultures)
            {
                FillData(data, LoadResourceData(name, assembly, cultureInfo, out var cultureLoaded));
                if (cultureLoaded)
                {
                    loaded = true;
                }
            }

            return loaded;
        }

        private static Dictionary<string, string> LoadResourceData(string name, Assembly assembly,
            CultureInfo cultureInfo, out bool loaded)
        {
            Assembly satelliteAssembly;
            if (Equals(cultureInfo, CultureInfo.InvariantCulture))
            {
                satelliteAssembly = assembly;
            }
            else
            {
                try
                {
                    satelliteAssembly = assembly.GetSatelliteAssembly(cultureInfo);
                }
                catch (FileNotFoundException exception)
                {
                    _logger.LogWarning(exception,
                        "Could not find satellite assembly for '{CultureInfoName}': {Message}",
                        cultureInfo.Name, exception.Message);
                    loaded = false;
                    return new Dictionary<string, string>();
                }
            }

            var resourceName = satelliteAssembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith($"{name}.json"));
            if (string.IsNullOrEmpty(resourceName))
            {
                loaded = false;
                return new Dictionary<string, string>();
            }

            var stream = satelliteAssembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                loaded = false;
                return new Dictionary<string, string>();
            }

            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();
            loaded = true;
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }
    }
}
