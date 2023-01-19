using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App.Web;

namespace Sitko.Core.Blazor.Server;

public abstract class BlazorApplication<TStartup> : WebApplication<TStartup> where TStartup : BlazorStartup
{
    protected BlazorApplication(string[] args) : base(args)
    {
        this.AddPersistentState();
        ConfigureServices(collection =>
        {
            collection.AddScriptInjector();
        });
    }
}
