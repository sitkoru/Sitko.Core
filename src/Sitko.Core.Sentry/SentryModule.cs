using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sentry.AspNetCore;
using Sitko.Core.App;

namespace Sitko.Core.Sentry;

public class SentryModule : BaseApplicationModule<SentryModuleOptions>,
    IHostBuilderModule<SentryModuleOptions>
{
    public override string OptionsKey => "Sentry";

    public void PostConfigureHostBuilder(IApplicationContext context, IHostApplicationBuilder hostBuilder, SentryModuleOptions startupOptions) =>
        hostBuilder.ConfigureWebHostDefaults(webHostBuilder =>
        {
            webHostBuilder.UseSentry(builder =>
            {
                startupOptions.ConfigureSentry?.Invoke(context, builder, startupOptions);
                builder.AddSentryOptions(o =>
                {
                    o.Dsn = startupOptions.Dsn;
                    o.Debug = startupOptions.EnableDebug;
                    o.TracesSampleRate = startupOptions.TracesSampleRate;
                    o.DefaultTags.Add("ServiceId", context.Id.ToString());
                    o.DefaultTags.Add("Service", context.Name);
                    o.DefaultTags.Add("Environment", context.Environment);
                    o.DefaultTags.Add("Version", context.Version);
                    startupOptions.ConfigureSentryOptions?.Invoke(context, o, startupOptions);
                });
            });
        });
}
