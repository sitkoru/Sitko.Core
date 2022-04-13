#if NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Blazor.Components;

public class PersistentStateModule : BaseApplicationModule<PersistentStateModuleOptions>
{
    public override string OptionsKey => "PersistentState";

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        PersistentStateModuleOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddScoped<IStateCompressor, StateCompressor>();
        services.AddScoped<ICompressedPersistentComponentState, CompressedPersistentComponentState>();
    }
}

public class PersistentStateModuleOptions : BaseModuleOptions
{
}

#endif

