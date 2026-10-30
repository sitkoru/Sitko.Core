# Redis, Valkey Sentinel and Cluster

[Русский](../ru/redis-sentinel.md) · [All topics](README.md)

`Sitko.Core.Redis` provides one Redis-compatible connection per application.
The standalone Redis module, Auth Data Protection and OIDC token storage share
one `RedisConnection` and one selected database within the DI container.
Separate applications own separate connections.

`RedisConnection` disposes the data transport and, in Sentinel mode, the separate
Sentinel transport when the container is disposed. Consumers obtain `IDatabase`
from DI or `IConnectionMultiplexer` through `RedisConnection.Connection` and must
not dispose them themselves. Settings are fixed when the application is built;
configuration changes require a restart. Network connections open on first DI access.

## Usage without Auth

Reference `Sitko.Core.Redis` and register the module:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Redis;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSitkoCore().AddRedis();
using var host = builder.Build();
var database = host.Services.GetRequiredService<IDatabase>();
await database.StringSetAsync("example", "value");
```

`RedisModuleOptions` binds `Redis:Connection`. An `AddRedis` callback can also set
these options; `optionsKey` selects another section for the same application-wide
connection, rather than creating a separate module connection.

## Connection and Auth configuration

Direct connection example (standalone or Cluster):

```json
{
  "Redis": {
    "Connection": {
      "Mode": "Direct",
      "Configuration": "redis.example.net:6379,user=app,password=example-data-password,defaultDatabase=0"
    }
  },
  "Auth": {
    "OIDC": {
      "Client": {
        "EnableRedisDataProtection": true,
        "TokenStoreType": "Redis"
      }
    }
  }
}
```

Example passwords are fictional; supply actual values through your secret source.
Provide other required OIDC settings as usual. Auth automatically registers the
shared connection when Redis Data Protection or the `Redis` token store is enabled.
No additional `AddRedis` call is needed. If present, Auth uses its connection
regardless of registration order. With both consumers disabled, Auth requires no
Redis settings and opens no Redis network connection.

| Setting | Purpose |
| --- | --- |
| `Mode` | `Direct` by default, or `Sentinel`. There is no separate Cluster mode. |
| `Configuration` | Native StackExchange.Redis string: initial addresses, `user`, `password`, `defaultDatabase`, TLS, timeouts and other client settings. |
| `SentinelConfiguration` | Separate Sentinel addresses and credentials; used only with `Mode=Sentinel`. |

For Sentinel, configure:

```json
{
  "Redis": {
    "Connection": {
      "Mode": "Sentinel",
      "Configuration": "serviceName=auth-primary,user=auth-app,password=example-data-password,defaultDatabase=3",
      "SentinelConfiguration": "sentinel-1.example.net:26379,sentinel-2.example.net:26379,sentinel-3.example.net:26379,user=sentinel-client,password=example-sentinel-password"
    }
  }
}
```

`serviceName` must match the group in `sentinel monitor`. Sentinel credentials are
not inherited from `Configuration`; credentials used by Sentinel processes to
access data (`sentinel auth-user`, `sentinel auth-pass`) are configured on Sentinel.
A nonempty group name and Sentinel addresses are required. Unknown modes, empty or
invalid strings fail when building an application that uses Redis. `Direct` ignores
`SentinelConfiguration` and rejects `serviceName` inside `Configuration`, preventing
the client from implicitly enabling Sentinel.

Sentinel must be reachable and know the group on initial connection. After connecting,
StackExchange.Redis discovers primary changes and reconnects. Individual operations
may fail during failover; this integration does not automatically retry arbitrary
application operations.

## Valkey Cluster and multiple databases

For Cluster, use `Mode=Direct` with one or more initial addresses in `Configuration`.
The client discovers other nodes automatically. All cluster-announced addresses must be reachable from
the application. After a primary changes, existing connections need time to update
routes. Individual operations can fail; the module adds no automatic application retries.

Set the database through `defaultDatabase` in `Configuration`. Databases other than DB 0 require Valkey
9.0 or later and `cluster-databases` on server nodes. For example,
`cluster-databases 4` permits DB 0–3; the default exposes only DB 0.
The `databases` setting does not apply in Cluster mode. Multi-key operations
require a shared hash slot; selecting another database does not remove this limit.
See the [Valkey Cluster specification](https://valkey.io/topics/cluster-spec/).

Use different databases for separate environments when application names match.
The database number separates identically named keys. Preserve Data Protection
key names and `ApplicationName` when switching servers or transferring the key ring.
The user token store uses SHA256 of `DOT_{ApplicationName}_{Environment}_{userId}`;
application and environment already form part of the key identity.

## Deployment requirements

For production Sentinel, use at least three Sentinels on independent nodes that
do not fail together. The data primary needs replicas. A single test container
does not provide this resilience.

The application must reach every primary, replica and Sentinel address announced
through discovery, including addresses after failover. Exposing initial Sentinel
addresses alone is insufficient. With NAT/port mapping, check advertised addresses
and ports: an address reachable by Sentinel inside its network may be unreachable
by the application. See [Valkey Sentinel](https://valkey.io/topics/sentinel/).

Data Protection keys require persistent storage: configure Valkey disk persistence
and `maxmemory-policy noeviction`. Losing keys prevents decryption of previously
issued protected data. Application replicas must share `ApplicationName` and
`defaultDatabase`. The key name is `{ApplicationName}-DP`, and Data Protection uses
`ApplicationName` as its application name. A shared database alone does not replace
matching application names.

Sentinel uses asynchronous replication and does not guarantee preservation of
every acknowledged write on failure. Persistence and `noeviction` do not remove
this limitation. Select acceptable data-loss requirements with
[Sentinel guarantees](https://valkey.io/topics/sentinel/) in mind.

The connection uses `StackExchange.Redis` `3.4.20`, including RESP3 by default.
Discovery options are described in the
[version's documentation](https://github.com/StackExchange/StackExchange.Redis/blob/3.4.20/docs/Configuration.md).

## Local verification

Use Docker with Docker Compose, stable .NET 10 SDK selected by `global.json`,
and .NET 10 runtime. Run commands from the repository root. Ports
`16379–16381`, `17379–17384`, `26379–26381` on `127.0.0.1` must be free.

`tests/Sitko.Core.Auth.Tests/Valkey/compose.yml` starts two containers:
three Valkey nodes and three Sentinels on `valkey/valkey:8.1.4-alpine`, plus a Cluster
with three primaries and three replicas on `valkey/valkey:9.0.3-alpine`.
Images are pinned by digest. Cluster exposes DB 0–3. Container addresses/ports
match published addresses; tests run on the Docker host, not in a neighboring
container. Environment passwords are test data only. Persistence is disabled;
this is not a production configuration.

```sh
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml up -d --wait --force-recreate
dotnet test --project tests/Sitko.Core.Auth.Tests/Sitko.Core.Auth.Tests.csproj --framework net10.0 --report-trx --report-trx-filename sentinel.trx --results-directory TestResults/sentinel-net10.0
```

Every run requires fresh containers: Sentinel failover stops the original primary;
the Cluster test promotes a replica. Recreate containers before repeating the run.
Tests are mandatory and fail when the environment is unavailable.

Coverage includes the shared connection and database, legacy-setting isolation,
invalid configuration, disabled Redis, transport ownership, direct connections and separate Sentinel authentication, data shared
between application instances and failover. Cluster verification covers DB 0/DB 3
isolation, Data Protection, user tokens and recovered reads/writes on original
connections after a primary changes. The Sentinel scenario waits for replication
before stopping the primary. Test Sentinel declares a node unavailable after five
seconds, leaving time beyond the normal one-second PING interval to reconnect
after a role change. Cluster performs managed failover of each primary in sequence.
Both scenarios also check preserved data with a fresh application instance and DI
container. After failover, tests wait for each mutating operation on the original
connection to recover within the client's normal role-check interval plus
30 seconds. After successful mutation, only its result is read.
This confirms the tested scenario, not losslessness for arbitrary concurrent
production writes.

After verification, save logs if necessary and stop the environment even if tests fail:

```sh
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml logs --no-color
docker compose -p sitko-sentinel-tests -f tests/Sitko.Core.Auth.Tests/Valkey/compose.yml down --volumes
```

CI job `redis` in `.github/workflows/main.yml` runs tests for `net10.0` in
`actions-container`. Each run gets fresh Valkey/Sentinel containers sharing the
job container's network namespace so advertised `127.0.0.1` addresses are reachable.
The job saves TRX to `redis-net10.0-test-results` and always requests logs and cleanup.
Local tests and CI do not verify a Kubernetes operator, zonal disks or migration
of existing data between servers.

Sources: [connection](../../src/Sitko.Core.Redis/RedisConnection.cs),
[options](../../src/Sitko.Core.Redis/RedisModuleOptions.cs),
[dependency versions](../../Directory.Packages.props).
