using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Blazor.Server;

namespace Sitko.Core.Blazor.MudBlazor.Server;

public class MudBlazorStartup : BlazorStartup
{
    public MudBlazorStartup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
        environment)
    {
    }
}
