using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Sentry.AspNetCore;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Sentry;

public class SentryModule : BaseApplicationModule<SentryModuleOptions>,
    IWebApplicationModule<SentryModuleOptions>
{
    public override string OptionsKey => "Sentry";

    public void ConfigureWebHost(IApplicationContext applicationContext, ConfigureWebHostBuilder webHostBuilder,
        SentryModuleOptions options) =>
        webHostBuilder.UseSentry(builder =>
        {
            options.ConfigureSentry?.Invoke(applicationContext, builder, options);
            builder.AddSentryOptions(o =>
            {
                o.Dsn = options.Dsn;
                o.Debug = options.EnableDebug;
                o.TracesSampleRate = options.TracesSampleRate;
                o.DefaultTags.Add("ServiceId", applicationContext.Id.ToString());
                o.DefaultTags.Add("Service", applicationContext.Name);
                o.DefaultTags.Add("Environment", applicationContext.Environment);
                o.DefaultTags.Add("Version", applicationContext.Version);
                options.ConfigureSentryOptions?.Invoke(applicationContext, o, options);
            });
        });
}
