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

`Email.Tests` and `Storage.Tests` provide abstract tests inherited by concrete suites;
`Tasks.Kafka.Tests` is a scaffold without concrete scenarios. They are libraries and
are not executed separately. Other test projects are MTP applications.
Integration projects require their services: [PDF](../pdf.en.md#testing-the-module),
[Redis/Valkey](../redis-sentinel.md) and the environments used by existing CI jobs.

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
