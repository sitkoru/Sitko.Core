using System.Reflection;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Blazor.Server;

public interface ISitkoCoreBlazorServerApplicationBuilder : ISitkoCoreBlazorApplicationBuilder, ISitkoCoreServerApplicationBuilder, ISitkoCoreWebApplicationBuilder
{
    ISitkoCoreBlazorServerApplicationBuilder AddForms<TAssembly>();
    ISitkoCoreBlazorServerApplicationBuilder AddForms(Assembly assembly);
}
