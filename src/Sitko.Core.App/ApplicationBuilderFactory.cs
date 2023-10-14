using System.Collections.Concurrent;

namespace Sitko.Core.App;

public static class ApplicationBuilderFactory
{
    private static readonly ConcurrentDictionary<int, ISitkoCoreApplicationBuilder> Builders = new();

    public static TSitkoCoreApplicationBuilder
        GetOrCreateApplicationBuilder<TApplicationBuilder, TSitkoCoreApplicationBuilder>(
            TApplicationBuilder applicationBuilder, Func<TApplicationBuilder, TSitkoCoreApplicationBuilder> create)
        where TSitkoCoreApplicationBuilder : ISitkoCoreApplicationBuilder where TApplicationBuilder : notnull
    {
        var appBuilder = Builders.GetOrAdd(applicationBuilder.GetHashCode(), _ => create(applicationBuilder));
        if (appBuilder is not TSitkoCoreApplicationBuilder typedBuilder)
        {
            throw new InvalidOperationException($"Application builder is not {typeof(TSitkoCoreApplicationBuilder)}");
        }

        return typedBuilder;
    }


    public static TSitkoCoreApplicationBuilder GetApplicationBuilder<TApplicationBuilder,
        TSitkoCoreApplicationBuilder>(TApplicationBuilder applicationBuilder)
        where TSitkoCoreApplicationBuilder : ISitkoCoreApplicationBuilder where TApplicationBuilder : notnull
    {
        if (Builders.ContainsKey(applicationBuilder.GetHashCode()))
        {
            var builder = Builders[applicationBuilder.GetHashCode()];
            if (builder is TSitkoCoreApplicationBuilder typedBuilder)
            {
                return typedBuilder;
            }

            throw new InvalidOperationException($"Application builder is not {typeof(TSitkoCoreApplicationBuilder)}");
        }

        throw new InvalidOperationException(
            $"Application builder wasn't created for this HostBuilder. Call .AddSitkoCore*()");
    }
}
