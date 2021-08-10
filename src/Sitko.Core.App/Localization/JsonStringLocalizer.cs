using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace Sitko.Core.App.Localization
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly Dictionary<string, string> data;

        public JsonStringLocalizer(Dictionary<string, string> data) => this.data = data;

        public LocalizedString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                return TryGetResource(name, out string value)
                    ? new LocalizedString(name, value, false)
                    : new LocalizedString(name, name, true);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                return TryGetResource(name, out string value)
                    ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), false)
                    : new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, name, arguments), true);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            data.Select(r => new LocalizedString(r.Key, r.Value));

        private bool TryGetResource(string name, out string value) => data.TryGetValue(name, out value!);

        public IStringLocalizer WithCulture(CultureInfo culture) =>
            throw new NotSupportedException(
                "Obsolete API. See: https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.localization.istringlocalizer.withculture");
    }
}
