# Redis, Valkey Sentinel and Cluster

[Русский](../ru/redis-sentinel.md) · [All topics](README.md)

`Sitko.Core.Redis` provides direct Redis-compatible connections, primary discovery
through Valkey Sentinel and connection disposal. `Sitko.Core.Auth` and
`Sitko.Core.Auth.OIDC` use it for Data Protection and token storage.
One data connection is shared by ASP.NET Core Data Protection keys and
`RedisTokenStore` within a module instance. In Sentinel mode, DI owns both
`ConnectionMultiplexer` connections: Sentinel and data. `StackExchange.Redis`
manages failover; there is no application polling loop for the primary.

Connections are created on first DI access. During initial connection, Sentinel
must be reachable and know `RedisServiceName`, otherwise connection fails.
After a successful connection, the library discovers a new primary and reconnects;
individual operations may fail during failover. The integration does not
automatically retry arbitrary application operations.

## Usage without Auth

Reference `Sitko.Core.Redis` and register the module:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Redis;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSitkoCore().AddRedis(options =>
{
    options.SentinelEndpoints = ["sentinel-1.example.net:26379", "sentinel-2.example.net:26379"];
    options.ServiceName = "app-primary";
    options.Db = 3;
});
using var host = builder.Build();
var database = host.Services.GetRequiredService<IDatabase>();
await database.StringSetAsync("example", "value");
```

`RedisModuleOptions` binds section `Redis`; `AddRedis` accepts `optionsKey` to select
another section. For a direct connection, set `Host` and `Port` instead of
`SentinelEndpoints` and `ServiceName`. Other options match Auth settings below
without the `Redis` prefix: `Db`, `User`, `Password`, `SentinelUser`, `SentinelPassword`.
The shared module registers `RedisConnection` and `IDatabase` in DI. The container
owns the connection; consumers must not dispose it manually.

Auth keeps its own sections and flat `Redis*` settings. Do not call `AddRedis` just
for Auth: it uses the shared `RedisConnection` with its authentication module's settings.

## Auth configuration

Example section `Auth:OIDC:Client`:

```json
{
  "Auth": {
    "OIDC": {
      "Client": {
        "EnableRedisDataProtection": true,
        "TokenStoreType": "Redis",
        "RedisSentinelEndpoints": [
          "sentinel-1.example.net:26379",
          "sentinel-2.example.net:26379",
          "sentinel-3.example.net:26379"
        ],
        "RedisServiceName": "auth-primary",
        "RedisDb": 3,
        "RedisUser": "auth-app",
        "RedisPassword": "example-data-password",
        "RedisSentinelUser": "sentinel-client",
        "RedisSentinelPassword": "example-sentinel-password"
      }
    }
  }
}
```

These passwords are fictional; supply real values through your application's
secret source. Supply other required OIDC settings as usual. `RedisServiceName`
must match the group name in `sentinel monitor`. Use `host:port` addresses
for `RedisSentinelEndpoints`.

| Setting | Purpose |
| --- | --- |
| `RedisSentinelEndpoints` | Initial Sentinel addresses; required together with `RedisServiceName`. |
| `RedisServiceName` | Group whose primary is discovered through Sentinel. |
| `RedisUser`, `RedisPassword` | Data-node credentials; username is optional for password authentication without a separate ACL user. |
| `RedisSentinelUser`, `RedisSentinelPassword` | Separate application credentials for Sentinel. |
| `RedisDb` | Database for data, Data Protection keys and tokens; does not apply to Sentinel. |
| `RedisHost`, `RedisPort` | Direct connection address; ignored in Sentinel mode. |

Sentinel credentials are **not inherited** from `RedisUser` or `RedisPassword`.
Set them explicitly when Sentinel requires authentication. Credentials used by
Sentinel processes to access data nodes (`sentinel auth-user`, `sentinel auth-pass`)
are configured on Sentinel itself.

With no Sentinel settings, direct connection uses `RedisHost`, `RedisPort`,
`RedisPassword`, `RedisDb`; `RedisUser` is also available for direct connections.
Do not supply only half of the Sentinel configuration: both a nonempty address
list and a nonempty group name are required.

## Valkey Cluster and multiple databases

For Cluster, set an initial node through the shared module's `Host`/`Port` or Auth's
`RedisHost`/`RedisPort`. Omit `ServiceName`/`RedisServiceName` and Sentinel address
lists; an empty service name does not disable Sentinel mode. The client discovers
other nodes automatically. All cluster-announced addresses must be reachable from
the application. After a primary changes, existing connections need time to update
routes. Individual operations can fail; the module adds no automatic application retries.

Set the database through `Db`/`RedisDb`. Databases other than DB 0 require Valkey
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
`RedisDb`. The key name is `{ApplicationName}-DP`, and Data Protection uses
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

Coverage includes direct connection, separate Sentinel authentication, data shared
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
