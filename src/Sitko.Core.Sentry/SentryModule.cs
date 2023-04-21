using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sentry.AspNetCore;
using Sitko.Core.App;

namespace Sitko.Core.Sentry;

public class SentryModule : BaseApplicationModule<SentryModuleOptions>,
    IHostBuilderModule<SentryModuleOptions>
{
    public override string OptionsKey => "Sentry";

    public void ConfigureHostBuilder(IApplicationContext context, IHostBuilder hostBuilder, SentryModuleOptions startupOptions) =>
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
                    startupOptions.ConfigureSentryOptions?.Invoke(context, o, startupOptions);
                });
            });
        });
}
