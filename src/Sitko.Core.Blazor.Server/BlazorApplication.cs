using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App.Web;

namespace Sitko.Core.Blazor.Server;

public abstract class BlazorApplication<TStartup> : WebApplication<TStartup> where TStartup : BlazorStartup
{
    protected BlazorApplication(string[] args) : base(args)
    {
#if NET6_0_OR_GREATER
        this.AddPersistentState();
#endif
        ConfigureServices(collection =>
        {
            collection.AddScriptInjector();
        });
    }
}
