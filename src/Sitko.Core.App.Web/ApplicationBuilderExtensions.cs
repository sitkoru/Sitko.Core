using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;

namespace Sitko.Core.App.Web
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder ConfigureLocalization(this IApplicationBuilder appBuilder, string defaultCulture,
            bool enableRequestLocalization = false,
            params string[] supportedCultures)
        {
            if (enableRequestLocalization)
            {
                var requestLocalizationOptions = new RequestLocalizationOptions();
                if (supportedCultures.Any())
                {
                    requestLocalizationOptions.AddSupportedCultures(supportedCultures.ToArray());
                    requestLocalizationOptions.AddSupportedUICultures(supportedCultures.ToArray());
                }

                if (!string.IsNullOrEmpty(defaultCulture))
                {
                    requestLocalizationOptions.SetDefaultCulture(defaultCulture);
                }

                appBuilder.UseRequestLocalization(requestLocalizationOptions);
            }
            else
            {
                if (!string.IsNullOrEmpty(defaultCulture))
                {
                    var cultureInfo = new CultureInfo(defaultCulture);

                    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                }
            }

            return appBuilder;
        }
    }
}
