using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;

namespace Sitko.Core.App.Web
{
    using System;
    using System.Collections.Generic;

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder ConfigureLocalization(this IApplicationBuilder appBuilder,
            IEnumerable<string> supportedCultures, string? defaultCulture = null)
        {
            var supportedCulturesArray = supportedCultures.ToArray();
            if (supportedCulturesArray.Length == 0)
            {
                throw new ArgumentException("Supported cultures list is empty");
            }

            var requestLocalizationOptions = new RequestLocalizationOptions();
            requestLocalizationOptions.AddSupportedCultures(supportedCulturesArray);
            requestLocalizationOptions.AddSupportedUICultures(supportedCulturesArray);

            if (!string.IsNullOrEmpty(defaultCulture))
            {
                requestLocalizationOptions.SetDefaultCulture(defaultCulture);
            }

            appBuilder.UseRequestLocalization(requestLocalizationOptions);

            return appBuilder;
        }

        public static IApplicationBuilder ConfigureLocalization(this IApplicationBuilder appBuilder,
            string defaultCulture)
        {
            if (string.IsNullOrEmpty(defaultCulture))
            {
                throw new ArgumentException("Culture name is empty", nameof(defaultCulture));
            }

            var cultureInfo = new CultureInfo(defaultCulture);

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            return appBuilder;
        }
    }
}
