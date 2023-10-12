using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Apm;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddQueueElasticApm(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.AddSitkoCore().AddQueueElasticApm();
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddQueueElasticApm(this SitkoCoreApplicationBuilder applicationBuilder) =>
        applicationBuilder.AddModule<QueueElasticApmModule>();
}
