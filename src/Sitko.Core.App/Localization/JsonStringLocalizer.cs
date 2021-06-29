using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace Sitko.Core.App.Localization
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly Dictionary<string, string> _data;

        public JsonStringLocalizer(Dictionary<string, string> data)
        {
            _data = data;
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
            return _data.Select(r => new LocalizedString(r.Key, r.Value));
        }

        private bool TryGetResource(string name, out string value)
        {
            return _data.TryGetValue(name, out value!);
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotSupportedException(
                "Obsolete API. See: https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.localization.istringlocalizer.withculture");
        }
    }
}
