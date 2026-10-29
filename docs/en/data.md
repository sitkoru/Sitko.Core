# Databases, repositories and search

[Русский](../ru/data.md) · [All topics](README.md)

Register these modules after [Sitko.Core](application.md).
`AppDbContext`, entities and repositories mentioned below are application types.

## Entity Framework Core

`Sitko.Core.Db` provides `IDbContextProvider<TDbContext>` and shared settings.
Reference `Sitko.Core.Db.Postgres` and call `AddPostgresDatabase<AppDbContext>()`
for your `DbContext`. Settings bind `Db:Postgres:Default` first, then
`Db:Postgres:AppDbContext`. Available settings include `Host` (`localhost`),
`Port` (`5432`), `Username` (`postgres`), `Password`, `Database` (`dbname`),
`Schema`, `SslMode` (`Disable`) and `ConnectionOptions`. Choose an appropriate
TLS mode and supply secrets externally for a deployed environment.
`VerifyCA`/`VerifyFull` use `SaslCertBase64` as the root certificate.

`AutoApplyMigrations=true` applies pending migrations at startup. Set it to `false`
when a separate process owns migrations; registering the module is not a safe
trial run against a production database. `MaxMigrationsApplyTryCount=1`.
Context and connection pooling are enabled by default and controlled separately
through `EnableContextPooling` and `EnableNpgsqlPooling`. `EnableSensitiveLogging=false`.

`Sitko.Core.Db.InMemory` uses `AddInMemoryDatabase<AppDbContext>()` and sections
`Db:InMemory:Default` and `Db:InMemory:AppDbContext`. It is EF InMemory, not a model
of PostgreSQL transactions: the ignored-transaction warning is suppressed.

`Sitko.Core.Repository` contains `IRepository`/`IRepositoryQuery`, entities,
filters and operation results. `Sitko.Core.Repository.EntityFrameworkCore`
connects them to EF: `AddEFRepositories<TAssembly>()` scans the supplied type's
assembly; its section is `Repositories:EF`. Implement your entity's repository
and resolve it through DI; check mutation results before continuing.
See the [EF tests](../../tests/Sitko.Core.Repository.EntityFrameworkCore.Tests).

Verify by saving an entity, reading it in a new scope, updating and deleting it;
check transaction rollback separately against real PostgreSQL.
`Sitko.Core.Repository.Grpc` contains `ApiRequestInfo` query conversions
(`ApplyRequestParams`) and type converters. It does not automatically expose an
arbitrary repository. Your application server owns filtering limits and data access;
see [gRPC/SD](grpc-service-discovery.md) for transport configuration.

## ClickHouse

`Sitko.Core.ClickHouse`: `AddClickHouse(...)`, section `ClickHouse`, settings
`Host=localhost`, `Port=8123`, `UserName=clickhouse`, required nonempty `Database`,
`Password`, `WithSsl=false`, `Settings`, `MaxConnectionsPerServer=1`.
Use the appropriate HTTP(S) port; certificate validation is enabled by default
(`DisableCertificatesValidation=false`).
`Sitko.Core.ClickHouse.Linq2db`: `AddClickHouseDb<TDbContext>()` for a
`BaseClickHouseDbContext` subclass, with sections `ClickHouse:Db:Default` and
`ClickHouse:Db:<type name>`. It requires a registered `ClickHouseModule`.

Verify reads and inserts in an isolated database. Environment and query examples:
[ADO](../../tests/Sitko.Core.ClickHouse.Tests),
[LINQ](../../tests/Sitko.Core.ClickHouse.Linq2db.Tests).

## OpenSearch

`Sitko.Core.Search` defines models, providers and `ISearcher<T>`.
Reference `Sitko.Core.Search.OpenSearch`, call `AddOpenSearch(...)` and register
your provider with `RegisterSearchProvider<...>()`.
The `Search:OpenSearch` section contains `Url` (`http://localhost:9200`), `Login`,
`Password`, `Prefix`, `Refresh` (`False`), `DisableCertificatesValidation` (`false`)
and highlighting/stemmer settings. This is a search module, not a logging sink.
`Sitko.Core.Repository.Search` connects repositories to search providers through
`AddSearchRepository()`.

Search providers initialize in the background and log failures; successful Host
startup does not mean an index is ready. With `Refresh=False`, writes need not
be immediately searchable. Verify indexing, search and multi-page reindexing on
an isolated index. See the [OpenSearch tests](../../tests/Sitko.Core.Search.OpenSearch.Tests).

Sources: [PostgreSQL](../../src/Sitko.Core.Db.Postgres/PostgresDatabaseModule.cs),
[PostgreSQL options](../../src/Sitko.Core.Db.Postgres/PostgresDatabaseModuleConfig.cs),
[EF](../../src/Sitko.Core.Repository.EntityFrameworkCore/EFRepositoriesModule.cs),
[ClickHouse](../../src/Sitko.Core.ClickHouse/ClickHouseModuleOptions.cs),
[search](../../src/Sitko.Core.Search/SearchModule.cs),
[OpenSearch](../../src/Sitko.Core.Search.OpenSearch/OpenSearchModuleOptions.cs).
