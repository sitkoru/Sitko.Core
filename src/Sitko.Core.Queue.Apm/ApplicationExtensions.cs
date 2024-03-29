using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Apm;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddQueueElasticApm(this IHostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.GetSitkoCore().AddQueueElasticApm();
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddQueueElasticApm(this ISitkoCoreApplicationBuilder applicationBuilder) =>
        applicationBuilder.AddModule<QueueElasticApmModule>();
}
