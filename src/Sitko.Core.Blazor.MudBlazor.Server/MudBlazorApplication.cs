using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Server;

namespace Sitko.Core.Blazor.MudBlazor.Server;

public class MudBlazorApplication<TStartup> : BlazorApplication<TStartup> where TStartup : MudBlazorStartup
{
    public MudBlazorApplication(string[] args) : base(args) => this.AddMudBlazor();
}
