# Running tests

[Русский](../ru/testing.md)

The repository uses the .NET SDK selected by [global.json](../../global.json),
xUnit `4.0.2`, Microsoft.Testing.Platform `2.5.1`, and AwesomeAssertions `9.6.0`.
Versions are declared in [Directory.Packages.props](../../Directory.Packages.props).
Run commands from the repository root.

```sh
dotnet test --project tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --framework net10.0 --minimum-expected-tests 1
dotnet test --project tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --list-tests
dotnet test --project tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --filter-class '*ConfigurationTests*' --minimum-expected-tests 1
```

Use `--filter-method` for a method and `--filter-trait` for a trait. Do not use
VSTest `--filter`. A nonzero exit code indicates failure; a successful process
without the expected scenarios does not establish a successful verification.

For TRX, add `--report-trx --report-trx-filename app.trx --results-directory TestResults/app`.
GitHub Actions adds `--report-github`; the extensions are referenced only by executable
projects in [tests/Directory.Build.props](../../tests/Directory.Build.props).
For diagnostics use `--diagnostic --diagnostic-output-directory TestResults/diagnostics`.
Code coverage collection is not configured.

`Email.Tests` provides abstract tests inherited by the SMTP suite and executable
delivery tests using `DevEmailModule`. `Storage.Tests` provides abstract tests,
is built as a library, and is not executed separately. Executable test projects
are MTP applications.

`Tasks.Kafka.Tests` verifies task lifecycle scenarios with Kafka and PostgreSQL. Its
[fixture](../../tests/Sitko.Core.Tasks.Kafka.Tests/KafkaFixture.cs) starts and disposes its
own containers; Docker must be available.

```sh
dotnet test --project tests/Sitko.Core.Tasks.Kafka.Tests/Sitko.Core.Tasks.Kafka.Tests.csproj --minimum-expected-tests 3
```

Integration projects require their services: [PDF](../pdf.en.md#testing-the-module),
[Redis/Valkey](../redis-sentinel.md) and the environments used by existing CI jobs.

## GitHub Actions verification

[Main](../../.github/workflows/main.yml) runs executable projects on `net10.0`
in subsystem jobs. This includes the concrete `App.Web.Tests`, `Grpc.Server.Tests`,
`Repository.Grpc.Tests`, `Repository.Tests`, and `Tasks.Kafka.Tests` suites.
`Email.Smtp.Tests` is excluded:
its existing scenario sends a real email and has no isolated SMTP fixture.

Each invocation requires at least one test and writes TRX and diagnostics to
`TestResults/<job>/<project>/net10.0/`; PDF uses separate `cdp` and `local`
subdirectories. Jobs upload available files even after failure as
`<job>-net10.0-test-results` artifacts retained for three days. A build failure
may produce no TRX; diagnostics are retained if the runner has written them.
Errors are not suppressed: a failing suite fails its job and blocks dependent publishing.

Main pins the SDK image by digest. PostgreSQL uses `pg_isready`; Consul,
MinIO, and Vault use HTTP probes with bounded retries; NATS waits for its TCP port
for at most 60 seconds. PDF, OpenSearch, and Redis/Valkey retain their existing
readiness checks. Kafka and ClickHouse use their own test fixtures.

A new PR run cancels an outdated run of the same PR and workflow. Cancellation
is disabled for push and release. [CodeQL](../../.github/workflows/codeql-analysis.yml)
checks pushes and PRs targeting `v13`. Main retains its release conditions.

Contract: [change verification](../../specs/20261028-110000-ci-verification/spec.md).

## Migrating test infrastructure consumers

`Sitko.Core.Xunit` and `Sitko.Core.Xunit.Web` provide environment helpers as libraries.
Reference `xunit.v3.mtp-v2` version `4.0.2` in each executable test project and set
`OutputType=Exe` and `UseMicrosoftTestingPlatformRunner=true`. Add this section beside
the SDK settings in `global.json`:

```json
"test": { "runner": "Microsoft.Testing.Platform" }
```

Remove `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` references when using
only MTP. Assertions use AwesomeAssertions; replace `FluentAssertions` namespaces
with `AwesomeAssertions`. `BaseTest`, `BaseTestScope`, and `WebTestScope` retain their APIs.

Contract: [specification](../../specs/20261028-103000-test-platform/spec.md).
