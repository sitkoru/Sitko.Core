using Sitko.Core.App.Web;

namespace Sitko.Core.App.Blazor
{
    public abstract class BlazorApplication<TStartup> : WebApplication<TStartup> where TStartup : BlazorStartup
    {
        protected BlazorApplication(string[] args) : base(args)
        {
        }
    }
}
