# Sitko.Core 13 documentation

[Русский](../ru/README.md)

Sitko.Core is a modular foundation for .NET 10 applications. Connect the features
your application needs; infrastructure servers and application models are prepared
separately. These guides describe version 13's branch; documentation in Git does
not imply that the corresponding NuGet packages have been published.

## Getting started

1. Check [SDK and build requirements](build-configuration.md).
2. Register the [application and modules](application.md).
3. Select features below and configure their dependencies.
4. When upgrading an application, read [migration from version 12](migration-v13.md).

The [Blazor demo](blazor-demo.md) shows forms, URLs and files without an external database.
[gRPC/SD](grpc-service-discovery.md) covers registration, configuration and TLS/Consul
requirements. The [gRPC demo](grpc-demo.md) calls an RPC through local Consul.
[Testing](testing.md) explains checks and test-environment limitations.

## Feature catalog

Packages in the table share the `Sitko.Core.` prefix. Each row links to a scenario
guide, grouping shared contracts and implementations within a related topic.

| Feature | Packages |
| --- | --- |
| [Application](application.md) | `App`, `App.Web` |
| [Vault](vault.md) | `Configuration.Vault` |
| [Logs, metrics, tracing](observability.md) | Included in `App` |
| [Sign-in and JWT validation](oidc-authentication.md) | `Auth`, `Auth.OIDC` |
| [Local accounts](identity.md) | `Identity` |
| [Redis and Valkey](redis-sentinel.md) | `Redis` |
| [Databases and repositories](data.md) | `Db`, `Db.InMemory`, `Db.Postgres`, `Repository`, `Repository.EntityFrameworkCore`, `Repository.Grpc`, `Repository.Search`, `ClickHouse`, `ClickHouse.Linq2db`, `Search`, `Search.OpenSearch` |
| [Queues and tasks](messaging.md) | `Queue`, `Queue.InMemory`, `Queue.Kafka`, `Kafka`, `Tasks`, `Tasks.Kafka` |
| [Files and images](storage.md) | `Storage`, `Storage.FileSystem`, `Storage.S3`, `Storage.Metadata.Postgres`, `Storage.Remote`, `Storage.Remote.Server`, `Storage.ImgProxy`, `ImgProxy` |
| [Email](email.md) | `Email`, `Email.Smtp` |
| [gRPC and service discovery](grpc-service-discovery.md) | `Grpc`, `Grpc.Client`, `Grpc.Server`, `ServiceDiscovery`, `ServiceDiscovery.Consul` |
| [Blazor](blazor.md) | `Blazor`, `Blazor.Server`, `Blazor.Wasm`, `Blazor.AntDesign`, `Blazor.AntDesign.Server`, `Blazor.MudBlazor`, `Blazor.MudBlazor.Server`, `Blazor.FluentValidation`, `Blazor.FileUpload` |
| [PDF and screenshots](pdf.md) | `Pdf` |
| [Test infrastructure](testing.md) | `Xunit`, `Xunit.Web` |

## Development resources

[Build configuration](build-configuration.md) · [Running tests](testing.md) ·
[Contributor instructions (Russian)](../contributing.md) ·
[Documentation guidelines (Russian)](../documentation-guidelines.md).
Specifications and process materials retain their established formats;
user-facing guides have matching RU/EN versions.
