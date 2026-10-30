# Migrating to Sitko.Core 13

[Русский](../ru/migration-v13.md)

Version 13 contains a breaking reduction of the library set. The packages below
are not released as part of 13; their types and registration methods are
unavailable. Check direct and transitive application dependencies and remove
these package references, namespaces and their former configuration.

## .NET 10 platform

Microsoft packages and the .NET integrations (Npgsql, Razor, MudBlazor, gRPC,
validation, logging and OpenTelemetry) use current compatible stable versions.
AntDesign remains at `0.20.4`; the OIDC module uses Duende `4.3.0`.
`OpenTelemetry.Instrumentation.GrpcNetClient` uses the
`1.20.1-beta.1` prerelease because the package has no stable release.

Sitko.Core 13 packages contain only `net10.0` assemblies. Applications targeting
.NET 8 must move to .NET 10 before upgrading these packages. Repository builds
use the stable SDK selected in [global.json](../../global.json); consumers need a
compatible .NET 10 SDK. Install the current .NET 10 runtime and ASP.NET Core 10
for web applications, and update application build environments and base container
images. .NET 11 support is outside this release.

Align direct Microsoft/ASP.NET Core/EF Core references with package 13 dependencies;
do not pin transitive dependencies to older versions. Microsoft package families
may have different version numbers. See [build configuration](build-configuration.md).

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

## AntDesign assets

`Sitko.Core.Blazor.AntDesign` retains its components, their API, and the
`AntDesign 0.20.4` dependency. Version 13 does not ship
`_content/Sitko.Core.Blazor.AntDesign/fileupload.js` or its additional upload-list
styles.

If your application explicitly loads this script, remove that reference.
The components use the base AntDesign styles. If your application needs the
previous additional rules for card sizes, file names, or action buttons,
maintain those styles in the application and verify its upload forms.

## Redis client and migration from v12

`Sitko.Core.Redis` uses `StackExchange.Redis 3.4.20`; align direct client references
with this version. The v13 connection format breaks compatibility with flat v12
settings: the [shared contract](redis-sentinel.md) lives under `Redis:Connection`.
Standalone Redis, Auth Data Protection and Redis token storage share one transport
and one database per application. There are no per-module connections or legacy fallbacks.

| Previous setting or API | Action for v13 |
| --- | --- |
| `Redis:Host/Port/Db/User/Password`, flat `Auth.Redis*` | Put addresses, `user`, `password`, `defaultDatabase` in `Redis:Connection:Configuration`. Old properties are removed from `RedisModuleOptions` and `AuthOptions`. |
| `SentinelEndpoints`, `ServiceName`, `SentinelUser`, `SentinelPassword` and `Auth.Redis*` equivalents | Set `Mode=Sentinel`; put `serviceName` and data credentials in `Configuration`, Sentinel addresses and credentials in `SentinelConfiguration`. |
| Keyed `RedisConnection` / `IDatabase` by AuthOptions type | Resolve shared `RedisConnection` / `IDatabase` without a key. Access the multiplexer through `RedisConnection.Connection`; consumers must not dispose it. |
| Overrides of `AuthOptionsValidator.RequiresRedis` | The registration condition is now in `AuthModule.RequiresRedis`; override it in a custom module when needed. |

`EnableRedisDataProtection`, `DataProtectionLifeTimeInMinutes` and `TokenStoreType`
are retained. Auth automatically registers Redis only when required.
Key names, token JSON and protection purpose do not change.

Keep existing values in a shared v12/v13 configuration document and add the new
section. Addresses in this example are illustrative:

```json
{
  "Redis": {
    "Host": "old-valkey",
    "Port": 6379,
    "Db": 0,
    "ConnectionString": "old-valkey:6379,defaultDatabase=0",
    "Connection": {
      "Mode": "Direct",
      "Configuration": "cluster-node-1:6379,cluster-node-2:6379,cluster-node-3:6379,defaultDatabase=0"
    }
  }
}
```

v12 keeps reading old fields and ignores the new nested section; v13 reads only the
new section. Also preserve old `Auth.Redis*`, `RedisPersistent` and other fields still
used by v12 applications. Do not replace their addresses with Cluster addresses.

1. Set the new Cluster addresses in the shared `Redis:Connection` section, keeping
   the old server addresses in the legacy fields for v12. Use `Mode=Direct` for
   Cluster, without `serviceName`; include credentials and a server-supported
   database in `Configuration`.
2. Before upgrading an application, copy the required Data Protection keys to the
   target database, preserving `ApplicationName`, environment and user claim.
   Key and protected-data formats are retained; the library does not copy data.
3. Upgrade the application to v13. It starts using Cluster from the new section;
   v12 applications continue using the old server. No individual address override
   is required for each application. Verify existing protected data, normal
   operations and recovery after a primary change.

In the Sitkoru scenario, the maintainer has confirmed that Data Protection keys
have already been copied; upgrading the version performs the Cluster cutover.
Library checks do not verify that live copy. Migrating caches, sessions and tokens
is not a mandatory step in this scenario; preserving such state requires a
separately agreed migration when needed.

After new Cluster writes, reverting to v12 does not copy current state back to the
old server. Do not merge nonzero databases into DB0 without a separate decision;
support for nonzero Cluster databases depends on the server. Updating Sitko.Core
does not migrate custom registrations in MYCG.Core, Communications.Core or
applications: inspect their direct connections separately. The library does not
change Vault or live data.

## OpenSearch search client

`Sitko.Core.Search.OpenSearch` uses `OpenSearch.Client 2.3.0` and
`OpenSearch.Net 2.3.0`. The [vendor compatibility matrix](https://github.com/opensearch-project/opensearch-net/blob/main/COMPATIBILITY.md) pairs client 2.x with
OpenSearch server 3.x. If an application uses these packages' types directly or
pins their versions, align its references with version 2.x and review the
[client breaking changes](https://github.com/opensearch-project/opensearch-net/blob/main/UPGRADING.md).

Sitko.Core module settings and public search operations are retained.
The client uses the default Utf8Json serializer; it does not automatically switch
to System.Text.Json. Before updating an application, verify index creation and
reinitialization, document writes, searches and deletions against the server
version in use. Updating the client package does not upgrade the OpenSearch server.

## Dependency updates and compatibility

`Sitko.Core.ClickHouse.Linq2db` uses `linq2db 6.5.1` instead of `6.3.0`.
The public `BaseClickHouseDbContext` derives from `DataConnection`, so query
translation changes also affect application queries. Review these cases:

- Date differences expressed as `TotalDays`/`TotalHours` and other units measure
  elapsed time rather than calendar boundaries crossed.
- Projection calculations over non-nullable numeric and `bool` fields from a missing
  `LEFT JOIN` row use the .NET default value; filters and aggregates retain
  SQL `NULL`. This change does not apply to value-converted fields.
- Set and column operations with different value converters can throw
  `LinqToDBException` when storage representations are incompatible. For identical
  delegate-based conversions, use expression-based `HasConversion`.
- Arguments of a `FormattableString` stored in a variable and used in a query
  become SQL parameters. Use `RawSqlString` where the database only accepts
  a literal in that position.

See the [LINQ2DB 6.4–6.5.1 release notes](https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-651)
for details and other changes.

`Storage` uses `MimeMapping 4.0.0`. `MimeUtility.GetMimeMapping(string)` is retained,
but the extension database and priority-based selection algorithm are updated,
so extensions associated with multiple MIME types can produce a different type. Check application-relevant extensions and MIME-based rules; see the
[database changes](https://github.com/zone117x/MimeMapping/releases/tag/v4.0.0).

`Grpc.Client` uses `OpenTelemetry.Instrumentation.GrpcNetClient 1.20.1-beta.1`
with OpenTelemetry core `1.20.1`. When migrating from `1.15.1-beta.1`, account for
semantic conventions `1.42.0` and context propagation regardless of
`SuppressDownstreamInstrumentation`, which can change the downstream server
span's parent. Review monitoring queries and rules that depend on RPC attributes
and span relationships against the [instrumentation release notes](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/4f2a49444969d8ffae3d46007ce533d28ad153d3/src/OpenTelemetry.Instrumentation.GrpcNetClient/CHANGELOG.md).

`Queue.Nats` retains `NATS.Client 1.1.0` with `STAN.Client 0.3.0`.
NATS `1.1.1–1.1.8` is binary-incompatible with this STAN version: replacing
`CreateConnection(string)` with an overload that has an additional parameter
causes `MissingMethodException`. Do not override this dependency independently;
see the [NATS API change](https://github.com/nats-io/nats.net.v1/compare/1.1.0...1.1.1).

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
| `Puppeteer` | Remove `AddPuppeteer()`, `Puppeteer` settings and dependencies on this module's `IBrowserProvider`. Register PuppeteerSharp directly for custom browser automation. PDF requires only `AddPdfRenderer()`; see the [PDF migration guide](pdf.md#migrating-to-sitkocore-13) for options and browser setup. |
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
[Blazor guide](blazor-demo.md) for startup and verification.

`Auth.IdentityServer` is replaced by `Auth.OIDC`. See the [OIDC/JWT guide](oidc-authentication.md)
for registration/configuration changes, Keycloak client-role admission and Google migration.

## Application verification

1. Remove direct references and check transitive references from shared libraries.
2. Replace removed registrations and configuration with the chosen integration.
3. Rebuild and run the application's authentication, access control, mapping,
   message delivery, logging, PDF and API documentation checks as applicable.
4. Verify operation in the target environment before updating the deployment.

The Sitko.Core change does not migrate applications or environments.
Library-set contract: [specification](../../specs/20261027-233345-legacy-modules/spec.md).

## Test infrastructure

Test projects use xUnit4.0.2, MTP2.5.2 and AwesomeAssertions9.6.0. See [test infrastructure migration](testing.md#migrating-test-infrastructure-consumers) for runner references, SDK configuration, namespaces and commands.

## gRPC clients and service discovery

Remove references to `Sitko.Core.Consul`, `Sitko.Core.ServiceDiscovery.Resolver.Consul`,
`Sitko.Core.ServiceDiscovery.Server` and `Sitko.Core.ServiceDiscovery.Server.Consul`.
Use `Sitko.Core.ServiceDiscovery.Consul` where discovery is required.
Replace `AddConsul()` and generic `AddServiceDiscovery<TRegistrar, TResolver>()` /
`AddServiceDiscovery<TResolver>()` with `AddConsulServiceDiscovery()`.
The previous resolver/registrar types and `sd:///` are removed.

Move `Consul:ConsulUri` to `ServiceDiscovery:Consul:Address` and configure DNS
addresses separately through `DnsServers`. Previous check/refresh settings in
seconds are not read: configure `Ttl`, `HeartbeatInterval`, `DnsRefreshPeriod`
and `DeregisterCriticalServiceAfter` according to their new roles. The new scheme
does not read old application/port registrations; update communicating servers
and clients together.

With discovery, Microsoft's standard `RoundRobin` selects endpoints. The previous
`LoadBalancingPolicy` does not control this path; `None`/`PickFirst` semantics
are not preserved.

Replace `AddExternalGrpcClient<T>` with `AddGrpcClient<T>`. Set a direct address
at `Grpc:Client:Contracts:<full protobuf name>:Address`; without an address,
the client requires a discovery provider. For Consul, use
`AddConsulServiceDiscovery()` and the `ServiceDiscovery:Consul` section.

Deriving from `GrpcClientModule<TClient, TOptions>` or `BaseGrpcServerModule<TOptions>`
is not supported. Configure clients through `AddGrpcClient<TClient>(...)` and
`ConfigureGrpcClients(...)`; configure servers through `AddGrpcServer(...)` and
service registration. The module types are `GrpcClientModule<TClient>` and `GrpcServerModule`.

Move `options.RegisterService<T>()` and `options.RegisterServiceWithGrpcWeb<T>()`
out of the options callback into the builder chain:

```csharp
app.AddGrpcServer(options => options.RequireSchemeAuthorization("Bearer"))
    .RegisterService<InvoiceGrpcService>()
    .RegisterServiceWithGrpcWeb<ReportsGrpcService>();
```

Use `ConfigureGrpcClients(...)` for shared client settings.
`Grpc:Client:Default` and `Grpc:Server:Default` hold shared configuration;
`Contracts:<full protobuf name>` holds individual overrides.

Remove `Grpc:Server:EnableServiceDiscovery`. The shared
`ServiceDiscovery:Enabled=false` setting disables discovery, publication and TTL.
Servers without SD and clients with direct addresses continue to work. A client
without an address fails startup when SD is disabled.

Client identity uses the full protobuf name, such as `accounting.v1.Invoice`,
rather than a short C# name. Move contract settings to full names. Registration,
TLS configuration and provider settings are covered in the
[gRPC/SD guide](grpc-service-discovery.md).

### Separate MYCG.Core migration

MYCG.Core is migrated in a separate task. A single `AddMycgGrpc()` should use
`ConfigureGrpcClients` to select `MYCG.Core.App.GrpcTokenProvider`, shared logging
settings and the application's certificate policy. Register the provider's
dependencies through regular DI with appropriate lifetimes. The method should
not implicitly enable a server or Consul.

Business clients and services can then use ordinary Sitko.Core registrations.
Replace static/SD selection wrappers, `GrpcClientsAddresses` and the static
`IsGrpcConfigured` flag with contract configuration and application-owned server
module state. Remove implicit SD registration from `AddMinimal`/`AddWeb`, and
replace forced reflection in the server wrapper with explicit application configuration.

Incoming JWT/OIDC authorization, domain protobuf types, converters and MYCG user
context remain separate concerns. This Sitko.Core change does not modify MYCG.Core
or validate an external identity provider.
