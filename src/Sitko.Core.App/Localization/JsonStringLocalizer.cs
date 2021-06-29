using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Localization
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly Lazy<Dictionary<string, string>> _fallbackResources;
        private readonly Lazy<Dictionary<string, string>> _defaultResources;
        private readonly ILogger<JsonStringLocalizer> _logger;
        private readonly Lazy<Dictionary<string, string>> _resources;

        public JsonStringLocalizer(string resourceName, Assembly resourceAssembly, CultureInfo cultureInfo,
            ILogger<JsonStringLocalizer> logger)
        {
            _resources = new Lazy<Dictionary<string, string>>(
                () => ReadResources(resourceName, resourceAssembly, cultureInfo, false));
            _fallbackResources = new Lazy<Dictionary<string, string>>(
                () => ReadResources(resourceName, resourceAssembly, cultureInfo.Parent, true));
            _defaultResources = new Lazy<Dictionary<string, string>>(
                () => ReadResources(resourceName, resourceAssembly, CultureInfo.InvariantCulture, true));
            _logger = logger;
        }

        public LocalizedString this[string name]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                return TryGetResource(name, out string value)
                    ? new LocalizedString(name, value, false)
                    : new LocalizedString(name, name, true);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                return TryGetResource(name, out string value)
                    ? new LocalizedString(name, string.Format(value, arguments), false)
                    : new LocalizedString(name, string.Format(name, arguments), true);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _resources.Value.Select(r => new LocalizedString(r.Key, r.Value));
        }

        private Dictionary<string, string> ReadResources(string resourceName, Assembly resourceAssembly,
            CultureInfo cultureInfo, bool isFallback)
        {
            Assembly satelliteAssembly;
            try
            {
                satelliteAssembly = !Equals(cultureInfo, CultureInfo.InvariantCulture)
                    ? resourceAssembly.GetSatelliteAssembly(cultureInfo)
                    : resourceAssembly;
            }
            catch (FileNotFoundException exception)
            {
                _logger.LogInformation(exception,
                    "Could not find satellite assembly for {IsFallback}'{CultureInfoName}': {Message}",
                    (isFallback ? "fallback " : ""), cultureInfo.Name, exception.Message);
                return new Dictionary<string, string>();
            }

            var names = satelliteAssembly.GetManifestResourceNames();
            var name = names.FirstOrDefault(n => n.EndsWith(resourceName));
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogInformation(
                    "Resource '{ResourceName}' not found for {IsFallback}'{CultureInfoName}'",
                    resourceName, (isFallback ? "fallback " : ""), cultureInfo.Name);
                return new Dictionary<string, string>();
            }

            var stream = satelliteAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                _logger.LogInformation(
                    "Resource '{ResourceName}' not found for {IsFallback}'{CultureInfoName}'",
                    resourceName, (isFallback ? "fallback " : ""), cultureInfo.Name);
                return new Dictionary<string, string>();
            }

            using StreamReader reader = new(stream);
            string json = reader.ReadToEnd();

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }

        private bool TryGetResource(string name, out string value)
        {
            return _resources.Value.TryGetValue(name, out value!) ||
                   _fallbackResources.Value.TryGetValue(name, out value!) ||
                   _defaultResources.Value.TryGetValue(name, out value!);
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotSupportedException(
                "Obsolete API. See: https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.localization.istringlocalizer.withculture");
        }
    }
}
