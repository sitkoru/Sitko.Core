using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Sitko.Core.App.Localization
{
    using System;

    public interface ILocalizationProvider
    {
        string Localize(string message);
        string Localize(string message, params object[] arguments);
        string this[string name] { get; }
        string this[string name, params object[] arguments] { get; }
        IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures);
    }

    // ReSharper disable once UnusedTypeParameter
    public interface ILocalizationProvider<T> : ILocalizationProvider
    {
    }

    public class LocalizationProvider<T> : ILocalizationProvider<T>
    {
        private readonly IStringLocalizer<T>? localizer;

        public LocalizationProvider(IStringLocalizer<T>? localizer = null) => this.localizer = localizer;

        public string Localize(string message) => Localize(message, Array.Empty<object>());

        public string Localize(string message, params object[] arguments)
        {
            if (localizer is not null)
            {
                var result = localizer[message, arguments];
                if (!string.IsNullOrEmpty(result.Value))
                {
                    return result.Value;
                }
            }

            return string.Format(CultureInfo.CurrentCulture, message, arguments);
        }

        public string this[string name] => Localize(name);
        public string this[string name, params object[] arguments] => Localize(name, arguments);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            localizer?.GetAllStrings(includeParentCultures) ?? Array.Empty<LocalizedString>();
    }
}
