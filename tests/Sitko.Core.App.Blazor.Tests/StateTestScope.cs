#if NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Xunit;

namespace Sitko.Core.App.Blazor.Tests;

public class StateTestScope : BaseTestScope
{
    protected override IServiceCollection ConfigureServices(IApplicationContext applicationContext,
        IServiceCollection services, string name) => base.ConfigureServices(applicationContext, services, name)
        .AddScoped<IStateCompressor, JsonHelperStateCompressor>();
}
#endif
