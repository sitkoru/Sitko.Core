using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace Sitko.Core.App.Localization
{
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
        private readonly IStringLocalizer<T>? _localizer;

        public LocalizationProvider(IStringLocalizer<T>? localizer = null)
        {
            _localizer = localizer;
        }

        public string Localize(string message)
        {
            return Localize(message, new object[0]);
        }

        public string Localize(string message, params object[] arguments)
        {
            return _localizer is not null ? _localizer[message, arguments]! : string.Format(message, arguments);
        }

        public string this[string name] => Localize(name);
        public string this[string name, params object[] arguments] => Localize(name, arguments);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _localizer?.GetAllStrings(includeParentCultures) ?? new LocalizedString[0];
        }
    }
}
