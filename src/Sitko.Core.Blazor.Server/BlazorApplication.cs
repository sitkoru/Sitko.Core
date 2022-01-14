using Microsoft.Extensions.DependencyInjection;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App.Web;
#if NET6_0_OR_GREATER
using Sitko.Core.Blazor.Components;
#endif

namespace Sitko.Core.Blazor.Server;

public abstract class BlazorApplication<TStartup> : WebApplication<TStartup> where TStartup : BlazorStartup
{
    protected BlazorApplication(string[] args) : base(args) =>
        ConfigureServices(collection =>
        {
            collection.AddScriptInjector();
#if NET6_0_OR_GREATER
            collection.AddScoped<CompressedPersistentComponentState>();
#endif
        });
}
