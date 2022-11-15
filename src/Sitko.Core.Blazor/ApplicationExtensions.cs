using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Blazor.Components;

namespace Sitko.Core.Blazor;

public static class ApplicationExtensions
{
    public static Application AddPersistentState<TCompressor, TComponentState>(this Application application)
        where TCompressor : class, IStateCompressor
        where TComponentState : class, ICompressedPersistentComponentState =>
        application.ConfigureServices(collection => collection.AddScoped<IStateCompressor, TCompressor>()
            .AddScoped<ICompressedPersistentComponentState, TComponentState>());

    public static Application AddPersistentState(this Application application) =>
        AddPersistentState<JsonHelperStateCompressor, CompressedPersistentComponentState>(application);
}
