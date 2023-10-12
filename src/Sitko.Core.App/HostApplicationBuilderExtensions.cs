using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App;

public static class HostApplicationBuilderExtensions
{
    private static readonly ConcurrentDictionary<IHostApplicationBuilder, SitkoCoreApplicationBuilder> Builders = new();

    public static SitkoCoreApplicationBuilder
        AddSitkoCore(this IHostApplicationBuilder builder) =>
        builder.AddSitkoCore(Array.Empty<string>());

    public static SitkoCoreApplicationBuilder AddSitkoCore(this IHostApplicationBuilder builder, string[] args) =>
        Builders.GetOrAdd(builder, _ => new SitkoCoreApplicationBuilder(builder, args));
}
