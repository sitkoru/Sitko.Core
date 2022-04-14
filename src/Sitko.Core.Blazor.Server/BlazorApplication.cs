using Microsoft.Extensions.DependencyInjection;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App.Web;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using Sitko.Core.Blazor.Components;
#endif

namespace Sitko.Core.Blazor.Server;

public abstract class BlazorApplication<TStartup> : WebApplication<TStartup> where TStartup : BlazorStartup
{
    protected BlazorApplication(string[] args) : base(args)
    {
#if NET6_0_OR_GREATER
        this.AddPersistentState<JsonHelperStateCompressor, CompressedPersistentComponentState>();
#endif
        ConfigureServices(collection =>
        {
            collection.AddScriptInjector();
        });
    }
}
