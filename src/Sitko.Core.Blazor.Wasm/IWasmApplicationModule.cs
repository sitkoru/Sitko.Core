using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Blazor.Wasm;

public interface IWasmApplicationModule
{
    void ConfigureHostBuilder(IApplicationContext applicationContext, WebAssemblyHostBuilder hostBuilder);
}
