# Applications and modules

[Русский](../ru/application.md) · [All topics](README.md)

`Sitko.Core.App` connects modules to the standard .NET Host. A module registers
dependencies, reads its configuration section and participates in the application
lifecycle. `Sitko.Core.App.Web` integrates ASP.NET Core.
Use .NET 10 and reference the projects/packages you need; see the repository's
[build guide](build-configuration.md) for toolchain requirements.

## Minimal server

In a `Microsoft.NET.Sdk.Web` project referencing `Sitko.Core.App.Web`:

```csharp
using Sitko.Core.App.Web;

var builder = WebApplication.CreateBuilder(args);
builder.AddSitkoCoreWeb();
var app = builder.Build();
app.MapSitkoCore();
app.MapGet("/hello", () => "Hello from Sitko.Core");
app.Run();
```

After startup, `/hello` returns the example text. For a process without HTTP,
create `Host.CreateApplicationBuilder(args)`, call `AddSitkoCore()` from
`Sitko.Core.App`, build the Host and call `RunAsync()`. `Build()` alone does not
initialize modules.

## Configuration and custom modules

The `Application` section contains `Name`, `Version`, `Environment` and
`EnableConsoleLogging`. Defaults come from the entry assembly and Host environment;
console logging is enabled in Development.
The server builder also reads optional `appsettings.local.json` without automatic
reload of that file. Keep secrets out of Git.

To implement a module, derive from `BaseApplicationModule<TOptions>` with
`TOptions : BaseModuleOptions, new()`, set `OptionsKey` and register it with
`AddModule<TModule, TOptions>()`. Register dependencies in `ConfigureServices`
and initialize/check the environment in `InitAsync`. `Enabled=false` disables
the module. By default, registering the same module type twice is rejected.
Requirements from `GetRequiredModules` are checked at startup; missing modules
prevent startup.

A module binds `OptionKeys` sections in order, then runs the options' `Configure`
and registration callback. Later scalar values replace earlier ones; collections
use the standard .NET binder, with no general list-replacement guarantee.
`optionsKey` selects another section. Check initialization settings after a restart;
`IOptionsMonitor` alone does not mean a module rebuilds connections or subscriptions
when settings change.

## HTTP and localization

`MapSitkoCore()` connects module middleware and health endpoints:
`/health/all`, `/health/startup`, `/healthz`, `/ready`. Tags determine the checks
included. Database/S3 checks are excluded from liveness/readiness by default,
so `/ready` does not prove that every external service is available.
Authentication is connected for registered auth modules; the application defines
access policies. See [OIDC/JWT](oidc-authentication.md).

`AddJsonLocalization()` from `Sitko.Core.App.Localization` connects JSON resources
to `IStringLocalizer`; `AddDefaultResource<T>()` selects shared resources.
`Localization:Json:CacheTimeInMinutes` defaults to 60. Missing translations return
the key itself. The [localization tests](../../tests/Sitko.Core.App.Tests/ILocalizationTests.cs)
show the resource format and inclusion. Configure HTTP culture selection through
`ConfigureLocalization(...)` from App.Web.

Sources: [builder](../../src/Sitko.Core.App/SitkoCoreBaseApplicationBuilder.cs),
[lifecycle](../../src/Sitko.Core.App/ApplicationLifecycle.cs),
[options](../../src/Sitko.Core.App/Helpers/OptionsHelper.cs),
[HTTP integration](../../src/Sitko.Core.App.Web/WebApplicationBuilderExtensions.cs).
