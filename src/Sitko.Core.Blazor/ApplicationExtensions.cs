using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Blazor.Components;

namespace Sitko.Core.Blazor;

public interface ISitkoCoreBlazorApplicationBuilder : ISitkoCoreApplicationBuilder
{
}

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddPersistentState<TCompressor, TComponentState>(
        this IHostApplicationBuilder hostApplicationBuilder)
        where TCompressor : class, IStateCompressor
        where TComponentState : class, ICompressedPersistentComponentState
    {
        hostApplicationBuilder.GetSitkoCore().AddPersistentState<TCompressor, TComponentState>();
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddPersistentState(this IHostApplicationBuilder hostApplicationBuilder) =>
        AddPersistentState<JsonHelperStateCompressor, CompressedPersistentComponentState>(hostApplicationBuilder);

    public static ISitkoCoreApplicationBuilder AddPersistentState<TCompressor, TComponentState>(
        this ISitkoCoreApplicationBuilder applicationBuilder)
        where TCompressor : class, IStateCompressor
        where TComponentState : class, ICompressedPersistentComponentState =>
        applicationBuilder.ConfigureServices(collection => collection.AddScoped<IStateCompressor, TCompressor>()
            .AddScoped<ICompressedPersistentComponentState, TComponentState>());

    public static ISitkoCoreApplicationBuilder
        AddPersistentState(this ISitkoCoreApplicationBuilder applicationBuilder) =>
        AddPersistentState<JsonHelperStateCompressor, CompressedPersistentComponentState>(applicationBuilder);
}
