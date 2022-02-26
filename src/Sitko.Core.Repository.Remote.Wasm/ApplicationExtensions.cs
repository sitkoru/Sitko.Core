using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote.Wasm;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddWasmHttpRepositoryTransport(this Application application,
        Action<HttpRepositoryTransportOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<WasmHttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure, optionsKey);

    public static Application AddWasmHttpRepositoryTransport(this Application application,
        Action<IApplicationContext, HttpRepositoryTransportOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<WasmHttpRepositoryTransportModule, HttpRepositoryTransportOptions>(configure, optionsKey);
}
