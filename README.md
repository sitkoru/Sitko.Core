# Sitko.Core

Opinionated framework on top of .NET 8 and .NET 10 with ASP.NET Core for building microservices.

Provides base application and modules for fast application building.

# Modules

- Logging
- Tracing
- Service discovery
- Database access
- Search
- Queue
- GRPC
- Storage
- Email
- Unit testing

# Local Test Services

Start the local infrastructure used by the integration-style test projects:

```bash
docker compose up -d consul nats postgres sonyflake opensearch minio vault redis
```

When running the CI-covered service-backed tests from the host, export the same key settings that CI uses:

```bash
export STORAGE__S3__TESTS3STORAGESETTINGS__SERVER=http://localhost:9000
export STORAGE__S3__TESTS3STORAGESETTINGS__ACCESSKEY=ptTYf7VkCVbUjAzn
export STORAGE__S3__TESTS3STORAGESETTINGS__SECRETKEY=RddqonEnrZZaCU7kkZszN9yiMFkX7rH3
export STORAGE__METADATA__POSTGRES__TESTS3STORAGESETTINGS__HOST=localhost
export STORAGE__METADATA__POSTGRES__TESTS3STORAGESETTINGS__USERNAME=postgres
export STORAGE__METADATA__POSTGRES__TESTS3STORAGESETTINGS__PASSWORD=123
export VAULT__URI=http://localhost:8200
export VAULT__TOKEN=twit3itPSAD0yok
export VAULT__MOUNTPOINT=secret
export Search__OpenSearch__Url=http://localhost:9200
export Search__OpenSearch__Login=admin
export Search__OpenSearch__Password=sikdadasDA123@ituDSSaMydfdssdss
export QUEUE__NATS__SERVERS__0=nats://127.0.0.1:4222
export QUEUE__NATS__CLUSTERNAME=test-cluster
export DB__POSTGRES__TESTDBCONTEXT__HOST=localhost
export DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=localhost
export DB__POSTGRES__TPHDBCONTEXT__HOST=localhost
export TESTS__USEPOSTGRES=true
export IDPROVIDER__SONYFLAKE__URI=http://localhost:8987
export PUPPETEER_EXECUTABLE_PATH="/Applications/Brave Browser.app/Contents/MacOS/Brave Browser"
```

Kafka tests are self-contained via Testcontainers and do not use `docker-compose.yml`.

For local NATS verification, prefer `127.0.0.1` over `localhost` to avoid IPv6 URI parsing issues in the legacy NATS client.

Non-CI/manual test suites may need additional secrets or services and are not part of the default local verification flow.

# Using

See [demo app](apps/Blazor/Sitko.Core.Apps.Blazor) for now.
