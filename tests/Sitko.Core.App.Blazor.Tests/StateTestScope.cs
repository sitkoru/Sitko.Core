using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Xunit;

namespace Sitko.Core.App.Blazor.Tests;

public class StateTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureServices(IHostApplicationBuilder builder, string name)
    {
        base.ConfigureServices(builder, name).Services.AddScoped<IStateCompressor, JsonHelperStateCompressor>();
        return builder;
    }
}
