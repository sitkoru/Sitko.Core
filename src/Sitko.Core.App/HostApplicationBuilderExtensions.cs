using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App;

public static class HostApplicationBuilderExtensions
{
    public static SitkoCoreApplicationBuilder AddSitkoCore(this IHostApplicationBuilder builder, string[] args) =>
        new(builder, args);
}
