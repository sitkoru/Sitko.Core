using Sitko.Core.App.Blazor;

namespace Sitko.Core.Blazor.MudBlazorComponents
{
    public class MudBlazorApplication<TStartup> : BlazorApplication<TStartup> where TStartup : MudBlazorStartup
    {
        public MudBlazorApplication(string[] args) : base(args)
        {
        }
    }
}
