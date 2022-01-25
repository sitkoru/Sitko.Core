using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Blazor.Wasm;

public class WasmApplicationContext : BaseApplicationContext
{
    private readonly IWebAssemblyHostEnvironment environment;

    public WasmApplicationContext(Application application, IConfiguration configuration,
        IWebAssemblyHostEnvironment environment)
        : base(application, configuration) =>
        this.environment = environment;

    public override string EnvironmentName => environment.Environment;
    public override bool IsDevelopment() => environment.IsDevelopment();

    public override bool IsProduction() => environment.IsProduction();
}
