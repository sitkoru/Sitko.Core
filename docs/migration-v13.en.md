# Migrating to Sitko.Core 13: removed modules

[Русский](migration-v13.md)

Version 13 contains a breaking reduction of the library set. The packages below
are not released as part of 13; their types and registration methods are
unavailable. Check direct and transitive application dependencies and remove
these package references, namespaces and their former configuration.

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

## Retained capabilities

`Blazor.FluentValidation`, `Identity`, `Queue.InMemory`, `Search.OpenSearch` and
`Pdf` are retained. Normal queue publishing and message processors do not require
the MediatR integration.

At this stage, `Repository.Remote`, `Repository.Remote.Wasm`,
`Repository.Remote.Server` and the `apps/Blazor` demo are retained; decoupling
belongs to a separate stage of version 13.

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
