# Migrating to Sitko.Core 13

[Русский](migration-v13.md)

Version 13 contains a breaking reduction of the library set. The packages below
are not released as part of 13; their types and registration methods are
unavailable. Check direct and transitive application dependencies and remove
these package references, namespaces and their former configuration.

## .NET 10 platform

Microsoft packages and the .NET integrations (Npgsql, Razor, MudBlazor, gRPC,
validation, logging and OpenTelemetry) use current compatible stable versions.
AntDesign remains at `0.20.4`; the Duende upgrade belongs to the separate OIDC
migration. The existing `OpenTelemetry.Instrumentation.GrpcNetClient` prerelease
is retained because that package has no stable release.

Sitko.Core 13 packages contain only `net10.0` assemblies. Applications targeting
.NET 8 must move to .NET 10 before upgrading these packages. Repository builds
use the stable SDK selected in [global.json](../global.json); consumers need a
compatible .NET 10 SDK. Install the current .NET 10 runtime and ASP.NET Core 10
for web applications, and update application build environments and base container
images. .NET 11 support is outside this release.

Align direct Microsoft/ASP.NET Core/EF Core references with package 13 dependencies;
do not pin transitive dependencies to older versions. Microsoft package families
may have different version numbers. See [build configuration](en/build-configuration.md).

When migrating from the .NET 8 package variant, `IEFRepository<TEntity>.UpdateAllAsync`
uses `Action<UpdateSettersBuilder<TEntity>>`. Replace chained `SetProperty` calls
with individual calls in a block:

```csharp
await repository.UpdateAllAsync(item => item.IsActive, setters =>
{
    setters.SetProperty(item => item.Title, "Updated");
    setters.SetProperty(item => item.Count, item => item.Count + 1);
});
```

This is the same API already exposed by the .NET 10 package variant. The platform
transition does not itself require database schema changes; review application
model changes and EF Core migrations separately.

## Redis client

`Sitko.Core.Redis` uses `StackExchange.Redis 3.4.20`. Align direct application
references to this client with the Sitko.Core dependencies. Sitko.Core connection
settings and the selection of direct, Sentinel or Cluster connections are retained.
Before updating a deployment, verify the connection mode in use, recovery after
a primary change, and access to tokens and Data Protection keys.

## OpenSearch search client

`Sitko.Core.Search.OpenSearch` uses `OpenSearch.Client 2.2.0` and
`OpenSearch.Net 2.2.0`. The [vendor compatibility matrix](https://github.com/opensearch-project/opensearch-net/blob/main/COMPATIBILITY.md) pairs client 2.x with
OpenSearch server 3.x. If an application uses these packages' types directly or
pins their versions, align its references with version 2.x and review the
[client breaking changes](https://github.com/opensearch-project/opensearch-net/blob/main/UPGRADING.md).

Sitko.Core module settings and public search operations are retained.
The client uses the default Utf8Json serializer; it does not automatically switch
to System.Text.Json. Before updating an application, verify index creation and
reinitialization, document writes, searches and deletions against the server
version in use. Updating the client package does not upgrade the OpenSearch server.

## Development tool annotations

Sitko.Core packages do not declare a direct `JetBrains.Annotations` dependency
or use its
`PublicAPI` and `UsedImplicitly` attributes. If an application received this
package transitively and uses its attributes, add a direct
`JetBrains.Annotations` reference or remove the unused application annotations.

The Blazor dependency graph may still include this package transitively through
`Sitko.Blazor.ScriptInjector`; removing the direct Sitko.Core reference does not
exclude dependencies of third-party libraries.

This change to development tool hints does not alter module registration or
runtime operations. Nullable analysis attributes from
`System.Diagnostics.CodeAnalysis` are retained.

## Removed modules and required changes

All module names in this table have the `Sitko.Core.` prefix.

| Removed modules | Application migration |
| --- | --- |
| `Auth.Basic` | Remove `AddBasicAuth()` and `Auth:Basic` settings. If Basic Auth is needed, register the authentication handler directly and configure access policies. |
| `Auth.Google` | Remove `AddGoogleAuth()` and `Auth:Google` settings. Register OIDC or the Google handler directly and migrate user access policies. Successful authentication alone does not replace the former allowed-user list. |
| `Automapper` | Remove the module's `AddAutoMapper()` registration. Register AutoMapper directly with the required assemblies and profiles, or migrate mappings to Mapperly. A third-party extension with the same name is not the removed Sitko.Core module. |
| `ElasticStack`, `Queue.Apm` | Remove `AddElasticStack()`, `AddQueueElasticApm()`, Elastic APM settings and its queue middleware registration. Configure the required OpenTelemetry export or register the integration directly. |
| `Email.MailGun` | Remove `AddMailGunEmail()` and `Email:Mailgun` settings. Register a Mailgun sender directly or use the retained `Email.Smtp` module. |
| `HangFire` | Remove `AddHangfirePostgres()` and usages of this module's types. Register Hangfire, its storage and dashboard directly if required. The third-party `Hangfire.Core` dependency is retained for `Email`. |
| `Health`, `Health.Telegram` | Remove `AddTelegramHealthReporter()`, `Health:Telegram` settings and inheritance from `BaseHealthCheckPublisher`. Implement notifications using the standard `IHealthCheckPublisher` if needed. `/healthz`, `/ready`, `/health/all` and `/health/startup` remain in `App.Web`. |
| `IdProvider`, `IdProvider.SonyFlake` | Remove `AddSonyFlakeIdProvider()` and `IdProvider:SonyFlake` settings. Replace usages of `IIdProvider`, `FakeIdProvider`, `RandomGenerator` and `IdGenerationException` with an application-specific implementation; no replacement ID generator is provided. |
| `MediatR` | Remove the module's `AddMediatR<TAssembly>()` registration. Register the library directly for normal MediatR usage. `QueueModuleOptions.TranslateMediatRNotification<T>()`, `TranslateMediatRTypes` and the `IMediatRModule` requirement are removed; publish messages explicitly through the retained `IQueue`. |
| `OpenSearch` | Remove `AddOpenSearchLogging()` and `OpenSearch:Logging` settings; configure OTEL log export or register the required sink directly. The `Search.OpenSearch` search module is retained. |
| `Puppeteer` | Remove `AddPuppeteer()`, `Puppeteer` settings and dependencies on this module's `IBrowserProvider`. Register PuppeteerSharp directly for custom browser automation. PDF requires only `AddPdfRenderer()`; see the [PDF migration guide](pdf.en.md#migrating-to-sitkocore-13) for options and browser setup. |
| `Search.ElasticSearch` | Remove `AddElasticSearch()` and `Search:Elastic` settings. Use a direct Elasticsearch integration or plan a separate migration to `Search.OpenSearch`; index and configuration migration is not automatic. |
| `Sentry` | Remove `AddSentry()` and module `Sentry` settings. Register the Sentry SDK directly and restore the required settings if needed. |
| `Swagger` | Remove the module's `AddSwagger()` registration and `Swagger` settings. Register OpenAPI generation and its documentation UI directly, including authentication descriptions and endpoint options. |
| `Repository.Remote`, `Repository.Remote.Wasm`, `Repository.Remote.Server` | Remove the three package references, `AddRemoteRepositories()`, `AddHttpRepositoryTransport()`, `AddWasmHttpRepositoryTransport()` and `Repositories:Remote`/`Repositories:Remote:Transports:Http` settings. Replace inheritance from `BaseRemoteRepositoryController` and remote repository usage with explicit client and server operations in the chosen API. Sending serialized LINQ queries through this HTTP transport is unsupported; no automatic compatible migration to another transport is provided. |

## Retained capabilities

`Blazor.FluentValidation`, `Identity`, `Queue.InMemory`, `Search.OpenSearch` and
`Pdf` are retained. Normal queue publishing and message processors do not require
the MediatR integration.

`Storage.Remote` and `Repository.Grpc` retain their separate contracts and
are outside the removed set. The `apps/Blazor` demo retains forms, validation,
navigation and file uploads without `Repository.Remote*`; see the
[Blazor guide](en/blazor-demo.md) for startup and verification.

Renaming `Auth.IdentityServer` to `Auth.OIDC` is a separate version 13 task.
This guide does not claim that the new package name is available before delivery.

## Application verification

1. Remove direct references and check transitive references from shared libraries.
2. Replace removed registrations and configuration with the chosen integration.
3. Rebuild and run the application's authentication, access control, mapping,
   message delivery, logging, PDF and API documentation checks as applicable.
4. Verify operation in the target environment before updating the deployment.

The Sitko.Core change does not migrate applications or environments.
Library-set contract: [specification](../specs/20261027-233345-legacy-modules/spec.md).

## Test infrastructure

Test projects use xUnit4.0.2, MTP2.5.1 and AwesomeAssertions9.6.0. See [test infrastructure migration](en/testing.md#migrating-test-infrastructure-consumers) for runner references, SDK configuration, namespaces and commands.
