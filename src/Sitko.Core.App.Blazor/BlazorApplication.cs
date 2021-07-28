using Sitko.Core.App.Web;

namespace Sitko.Core.App.Blazor
{
    using Sitko.Blazor.ScriptInjector;

    public abstract class BlazorApplication<TStartup> : WebApplication<TStartup> where TStartup : BlazorStartup
    {
        protected BlazorApplication(string[] args) : base(args) =>
            ConfigureServices(collection =>
            {
                collection.AddScriptInjector();
            });
    }
}
