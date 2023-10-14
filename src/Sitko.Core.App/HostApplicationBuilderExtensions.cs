using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App;

public static class HostApplicationBuilderExtensions
{
    public static ISitkoCoreApplicationBuilder
        GetSitkoCore(this IHostApplicationBuilder builder) => GetSitkoCore<ISitkoCoreApplicationBuilder>(builder);

    public static TSitkoCoreApplicationBuilder
        GetSitkoCore<TSitkoCoreApplicationBuilder>(this IHostApplicationBuilder builder)
        where TSitkoCoreApplicationBuilder : ISitkoCoreApplicationBuilder =>
        ApplicationBuilderFactory.GetApplicationBuilder<IHostApplicationBuilder, TSitkoCoreApplicationBuilder>(builder);

    public static ISitkoCoreServerApplicationBuilder AddSitkoCore(this HostApplicationBuilder builder) =>
        builder.AddSitkoCore(Array.Empty<string>());

    public static ISitkoCoreServerApplicationBuilder AddSitkoCore(this HostApplicationBuilder builder, string[] args) =>
        ApplicationBuilderFactory.GetOrCreateApplicationBuilder(builder,
            applicationBuilder => new SitkoCoreServerApplicationBuilder(applicationBuilder, args));
}
