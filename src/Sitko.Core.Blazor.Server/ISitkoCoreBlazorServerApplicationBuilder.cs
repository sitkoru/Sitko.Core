using System.Reflection;
using Sitko.Core.App;

namespace Sitko.Core.Blazor.Server;

public interface ISitkoCoreBlazorServerApplicationBuilder : ISitkoCoreBlazorApplicationBuilder, ISitkoCoreServerApplicationBuilder
{
    ISitkoCoreBlazorServerApplicationBuilder AddForms<TAssembly>();
    ISitkoCoreBlazorServerApplicationBuilder AddForms(Assembly assembly);
    ISitkoCoreBlazorServerApplicationBuilder ForceAuthorization();
}
