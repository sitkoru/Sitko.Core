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

### Multiple PostgreSQL hosts

`Host` accepts a comma-separated list: `pg1,pg2,pg3`. `Port` supplies the shared
port; specify different ports in `Host`, for example `pg1:5432,pg2:5433`.
The module passes the list and `ConnectionOptions` to Npgsql. A suitable host is
selected when a connection opens; if the first host is unavailable, Npgsql tries the next.

Example for a context that writes data and applies migrations:

```json
{
  "Db": {
    "Postgres": {
      "AppDbContext": {
        "Host": "pg1,pg2,pg3",
        "Port": 5432,
        "Username": "app",
        "Database": "app",
        "ConnectionOptions": {
          "Target Session Attributes": "read-write",
          "Load Balance Hosts": false,
          "Host Recheck Seconds": 10
        }
      }
    }
  }
}
```

These are example settings, not new defaults. Supply the password from an external
configuration source and select TLS settings for your environment.
`read-write` requires a server that accepts writes with
`default_transaction_read_only` disabled. Without an explicit setting, Npgsql uses
`PGTARGETSESSIONATTRS` from the environment, or `Any` if it is absent. `Any` can select a replica;
`PreferPrimary` falls back to a replica when no primary is available and does not
guarantee writes. `Load Balance Hosts=false` preserves the host order.
`Host Recheck Seconds` controls how long a cached host state remains valid;
it is not a background polling interval.

The module and Npgsql do not configure replication or promote a new primary.
An interrupted command or transaction fails: listing multiple hosts does not
replay it on another server. To balance reads, use a separately configured
connection to replicas with explicitly chosen Npgsql settings; do not enable
such balancing for a context used for writes and migrations.

The [integration test](../../tests/Sitko.Core.Tasks.Kafka.Tests/PostgresMultiHostTests.cs)
executes a query through the module with an unavailable first endpoint and an
available second endpoint. It reuses PostgreSQL from the Kafka Tasks fixture and
does not model replication or primary promotion. See Npgsql's
[host selection](https://www.npgsql.org/doc/failover-and-load-balancing.html) and
[connection parameters](https://www.npgsql.org/doc/connection-string-parameters.html).

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

When registered through `AddOpenSearch(...)`, searchers remain `Scoped`, while
`OpenSearchClientProvider` shares one client and its serializer cache across DI
scopes and search model types within the application. Client creation is
synchronized. After `Url`, `Login`, `Password` or
`DisableCertificatesValidation` changes, the next operation receives a new
client; a previously obtained client remains usable to finish its operation.
Old clients are not retained in a separate configuration cache.
All steps of one index initialization use the same client, even if connection
settings change between closing and reopening the index.

`EnableClientLogging` takes effect without recreating the client; detailed
request and response logs use the `OpenSearchClientProvider` category when using
DI. Searchers continue to read index, highlighting and stemmer options.
The existing two-parameter `OpenSearchSearcher<T>` constructor is retained for
direct construction; shared clients are supplied by module registration.

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
