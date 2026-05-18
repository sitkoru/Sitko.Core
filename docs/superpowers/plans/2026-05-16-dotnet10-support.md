# .NET 10 Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the repository to the .NET 10 SDK, add `net10.0` support while preserving `net8.0` support for reusable libraries, update dependencies, align local test infrastructure with CI, and run CI tests for both runtimes.

**Architecture:** Shared framework defaults stay centralized in `Directory.Build.props` and package versions stay centralized in `src/Directory.Packages.props`. Reusable libraries and tests become multi-targeted to `net8.0;net10.0`, while demo/perf applications move to `net10.0` only. CI remains grouped by subsystem but explicitly runs tests for both target frameworks.

**Tech Stack:** .NET SDK 10, MSBuild props/CPM, GitHub Actions, Docker Compose, xUnit v3, ASP.NET Core, EF Core, Blazor, gRPC, OpenSearch, Postgres, MinIO, Vault, Consul, Kafka, NATS Streaming.

---

## File Map

**Repository-level build and SDK files**
- Modify: `global.json`
- Modify: `src/Directory.Build.props`
- Modify: `tests/Directory.Build.props`
- Modify: `src/Directory.Packages.props`
- Modify: `README.md`

**Projects with known explicit target frameworks**
- Modify: `src/Sitko.Core.ServiceDiscovery/Sitko.Core.ServiceDiscovery.csproj`
- Modify: `src/Sitko.Core.ServiceDiscovery.Server/Sitko.Core.ServiceDiscovery.Server.csproj`
- Modify: `src/Sitko.Core.ServiceDiscovery.Server.Consul/Sitko.Core.ServiceDiscovery.Server.Consul.csproj`
- Modify: `src/Sitko.Core.ServiceDiscovery.Resolver.Consul/Sitko.Core.ServiceDiscovery.Resolver.Consul.csproj`
- Modify: `tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj`
- Modify: `apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj`
- Modify: `apps/Blazor/Sitko.Core.Apps.Blazor/Sitko.Core.Apps.Blazor.csproj`
- Modify: `apps/Blazor/Sitko.Core.Apps.Blazor.Client/Sitko.Core.Apps.Blazor.Client.csproj`
- Modify: `apps/Blazor/Sitko.Core.Apps.Blazor.Data/Sitko.Core.Apps.Blazor.Data.csproj`
- Modify: `perf/Sitko.Core.App.Perf/Sitko.Core.App.Perf.csproj`

**CI and local infra**
- Modify: `.github/workflows/main.yml`
- Modify: `.github/workflows/release.yml`
- Modify: `docker-compose.yml`

**Planning and spec artifacts**
- Reference: `docs/superpowers/specs/2026-05-16-dotnet10-support-design.md`
- Create: `docs/superpowers/plans/2026-05-16-dotnet10-support.md`

### Task 1: Establish .NET 10 SDK Baseline and Multi-Target Defaults

**Files:**
- Modify: `global.json`
- Modify: `src/Directory.Build.props`
- Modify: `tests/Directory.Build.props`
- Modify: `src/Sitko.Core.ServiceDiscovery/Sitko.Core.ServiceDiscovery.csproj`
- Modify: `src/Sitko.Core.ServiceDiscovery.Server/Sitko.Core.ServiceDiscovery.Server.csproj`
- Modify: `src/Sitko.Core.ServiceDiscovery.Server.Consul/Sitko.Core.ServiceDiscovery.Server.Consul.csproj`
- Modify: `src/Sitko.Core.ServiceDiscovery.Resolver.Consul/Sitko.Core.ServiceDiscovery.Resolver.Consul.csproj`
- Modify: `tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj`

- [ ] **Step 1: Write the failing baseline check**

Run:

```bash
dotnet --version && dotnet build Sitko.Core.sln -c Release --no-incremental
```

Expected: the repo resolves through `global.json` to .NET 8 and either builds only `net8.0` targets or fails later when .NET 10-specific changes are introduced.

- [ ] **Step 2: Verify the baseline behavior before edits**

Run:

```bash
dotnet --version && dotnet build Sitko.Core.sln -c Release --no-incremental
```

Expected: the SDK version is `8.0.x`, confirming the repo is still pinned to the old SDK baseline.

- [ ] **Step 3: Update the repo-wide SDK and target framework defaults**

Change `global.json` to a concrete installed .NET 10 SDK, for example:

```json
{
  "sdk": {
    "version": "10.0.201",
    "rollForward": "latestFeature",
    "allowPrerelease": true
  }
}
```

Change `src/Directory.Build.props` from:

```xml
<TargetFramework>net8.0</TargetFramework>
```

to:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Change `tests/Directory.Build.props` from:

```xml
<TargetFramework>net8.0</TargetFramework>
```

to:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Update the four service discovery library projects and `tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj` from local `TargetFramework` to local `TargetFrameworks` so they follow the same library/test strategy.

- [ ] **Step 4: Run focused restore/build to verify the new defaults fail or pass for the right reason**

Run:

```bash
dotnet --version && dotnet build Sitko.Core.sln -c Release --no-incremental
```

Expected: the SDK version is `10.0.x`, and any failures are now genuine migration issues rather than old SDK pinning.

- [ ] **Step 5: Commit the baseline migration**

Run:

```bash
git add global.json src/Directory.Build.props tests/Directory.Build.props src/Sitko.Core.ServiceDiscovery/Sitko.Core.ServiceDiscovery.csproj src/Sitko.Core.ServiceDiscovery.Server/Sitko.Core.ServiceDiscovery.Server.csproj src/Sitko.Core.ServiceDiscovery.Server.Consul/Sitko.Core.ServiceDiscovery.Server.Consul.csproj src/Sitko.Core.ServiceDiscovery.Resolver.Consul/Sitko.Core.ServiceDiscovery.Resolver.Consul.csproj tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj
git commit -m "feat!: move repo baseline to .NET 10 SDK"
```

### Task 2: Update CPM Versions and Resolve net8/net10 Package Splits

**Files:**
- Modify: `src/Directory.Packages.props`
- Test: `src/Directory.Packages.props` via restore/build output

- [ ] **Step 1: Write the failing package resolution check**

Run:

```bash
dotnet restore Sitko.Core.sln
```

Expected: restore or later build should expose the package/version gaps created by the new `net10.0` targets.

- [ ] **Step 2: Verify restore shows the unresolved state before CPM edits**

Run:

```bash
dotnet restore Sitko.Core.sln
```

Expected: either restore warnings/errors for unsupported packages on `net10.0`, or a restore that still needs build-time package adjustments.

- [ ] **Step 3: Update central package versions while preserving CPM formatting**

Edit `src/Directory.Packages.props` in place:

- keep comments and logical sections in their current order;
- keep unconditional `PackageVersion` items for packages that can be upgraded without losing `net8.0` compatibility;
- keep or expand conditional `ItemGroup Condition=" '$(TargetFramework)' == 'net8.0' "` blocks for packages that need different versions;
- add corresponding `net10.0` conditional blocks where a newer major/minor is needed only for `.NET 10`.

Use this shape when splitting versions by TFM:

```xml
<ItemGroup Condition=" '$(TargetFramework)' == 'net8.0' ">
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="9.0.6" />
</ItemGroup>
<ItemGroup Condition=" '$(TargetFramework)' == 'net10.0' ">
  <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.x" />
</ItemGroup>
```

Apply the same pattern to any `Microsoft.Extensions.*`, `Microsoft.AspNetCore.*`, EF Core, test SDK, or other packages that require divergence.

- [ ] **Step 4: Run restore and build to verify CPM changes**

Run:

```bash
dotnet restore Sitko.Core.sln && dotnet build Sitko.Core.sln -c Release --no-restore
```

Expected: package restore succeeds under both TFMs, and build failures now point to source/API migration issues rather than package resolution.

- [ ] **Step 5: Commit the CPM migration**

Run:

```bash
git add src/Directory.Packages.props
git commit -m "feat!: add net10 package matrix in central package management"
```

### Task 3: Move Demo and Perf Projects to net10.0 and Fix Build Breaks

**Files:**
- Modify: `apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj`
- Modify: `apps/Blazor/Sitko.Core.Apps.Blazor/Sitko.Core.Apps.Blazor.csproj`
- Modify: `apps/Blazor/Sitko.Core.Apps.Blazor.Client/Sitko.Core.Apps.Blazor.Client.csproj`
- Modify: `apps/Blazor/Sitko.Core.Apps.Blazor.Data/Sitko.Core.Apps.Blazor.Data.csproj`
- Modify: `perf/Sitko.Core.App.Perf/Sitko.Core.App.Perf.csproj`
- Modify: source files reported by `dotnet build` after the TFM and package updates

- [ ] **Step 1: Write the failing app build check**

Run:

```bash
dotnet build apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj -c Release && dotnet build apps/Blazor/Sitko.Core.Apps.Blazor/Sitko.Core.Apps.Blazor.csproj -c Release && dotnet build perf/Sitko.Core.App.Perf/Sitko.Core.App.Perf.csproj -c Release
```

Expected: current app/perf projects still target `net8.0` and may fail once package/framework changes are introduced.

- [ ] **Step 2: Verify the app/perf projects fail or remain old-targeted before edits**

Run:

```bash
dotnet build apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj -c Release && dotnet build apps/Blazor/Sitko.Core.Apps.Blazor/Sitko.Core.Apps.Blazor.csproj -c Release && dotnet build perf/Sitko.Core.App.Perf/Sitko.Core.App.Perf.csproj -c Release
```

Expected: apps are still on `net8.0`, confirming this task has not yet been applied.

- [ ] **Step 3: Move apps and perf projects to net10.0 and fix compilation issues**

Update each of these files from:

```xml
<TargetFramework>net8.0</TargetFramework>
```

to:

```xml
<TargetFramework>net10.0</TargetFramework>
```

for:

- `apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj`
- `apps/Blazor/Sitko.Core.Apps.Blazor/Sitko.Core.Apps.Blazor.csproj`
- `apps/Blazor/Sitko.Core.Apps.Blazor.Client/Sitko.Core.Apps.Blazor.Client.csproj`
- `apps/Blazor/Sitko.Core.Apps.Blazor.Data/Sitko.Core.Apps.Blazor.Data.csproj`
- `perf/Sitko.Core.App.Perf/Sitko.Core.App.Perf.csproj`

Then fix any compile-time breakages reported by `dotnet build`, including API obsoletions, package namespace changes, or framework behavior updates.

- [ ] **Step 4: Run the app/perf builds again**

Run:

```bash
dotnet build apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj -c Release && dotnet build apps/Blazor/Sitko.Core.Apps.Blazor/Sitko.Core.Apps.Blazor.csproj -c Release && dotnet build perf/Sitko.Core.App.Perf/Sitko.Core.App.Perf.csproj -c Release
```

Expected: all three app/perf build entry points succeed on `net10.0`.

- [ ] **Step 5: Commit the app/runtime migration**

Run:

```bash
git add apps/gRPC/Sitko.Core.Apps.Grpc/Sitko.Core.Apps.Grpc.csproj apps/Blazor/Sitko.Core.Apps.Blazor/Sitko.Core.Apps.Blazor.csproj apps/Blazor/Sitko.Core.Apps.Blazor.Client/Sitko.Core.Apps.Blazor.Client.csproj apps/Blazor/Sitko.Core.Apps.Blazor.Data/Sitko.Core.Apps.Blazor.Data.csproj perf/Sitko.Core.App.Perf/Sitko.Core.App.Perf.csproj
git add -u
git commit -m "feat!: move demo applications to net10"
```

### Task 4: Update CI to Use .NET 10 SDK and Run Tests for Both TFMs

**Files:**
- Modify: `.github/workflows/main.yml`
- Modify: `.github/workflows/release.yml`

- [ ] **Step 1: Write the failing CI design check**

Read the workflow behavior against the new requirements and confirm the current gap:

Run:

```bash
dotnet test tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --framework net8.0 && dotnet test tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --framework net10.0
```

Expected: local commands demonstrate that CI needs explicit per-framework runs rather than a single implicit test pass.

- [ ] **Step 2: Verify the current workflow structure is single-runtime**

Review `.github/workflows/main.yml` and `.github/workflows/release.yml` and confirm they currently rely on a single SDK/runtime path.

Expected: there is no explicit `net8.0`/`net10.0` dual-run test coverage yet.

- [ ] **Step 3: Update GitHub Actions workflows**

Edit `.github/workflows/main.yml` to:

- keep the existing job grouping by subsystem;
- run all library/test jobs under the .NET 10 SDK environment;
- invoke each relevant test project for both frameworks, for example:

```yaml
- name: Run tests net8.0
  run: dotnet test --logger GitHubActions --framework net8.0 tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj
- name: Run tests net10.0
  run: dotnet test --logger GitHubActions --framework net10.0 tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj
```

Update `.github/workflows/release.yml` only where required to ensure release build/pack continues on the new .NET 10 SDK baseline.

- [ ] **Step 4: Run representative dual-framework tests locally**

Run:

```bash
dotnet test tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --framework net8.0 && dotnet test tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --framework net10.0 && dotnet test tests/Sitko.Core.ImgProxy.Tests/Sitko.Core.ImgProxy.Tests.csproj --framework net8.0 && dotnet test tests/Sitko.Core.ImgProxy.Tests/Sitko.Core.ImgProxy.Tests.csproj --framework net10.0
```

Expected: representative CI-style tests pass for both TFMs.

- [ ] **Step 5: Commit the CI update**

Run:

```bash
git add .github/workflows/main.yml .github/workflows/release.yml
git commit -m "feat!: run CI tests for net8 and net10"
```

### Task 5: Align docker-compose with CI Service Topology and Verify Full Repository State

**Files:**
- Modify: `docker-compose.yml`
- Modify: `README.md`

- [ ] **Step 1: Write the failing integration-environment check**

Run:

```bash
dotnet test tests/Sitko.Core.Storage.S3.Tests/Sitko.Core.Storage.S3.Tests.csproj --framework net10.0 && dotnet test tests/Sitko.Core.Configuration.Vault.Tests/Sitko.Core.Configuration.Vault.Tests.csproj --framework net10.0
```

Expected: tests require external services and will fail or be unreliable until local compose matches the CI-backed environment.

- [ ] **Step 2: Verify the current compose file is stale**

Compare `docker-compose.yml` to `.github/workflows/main.yml` service definitions.

Expected: image names, versions, credentials, or runtime options differ for services such as MinIO, Vault, Postgres, NATS Streaming, and OpenSearch.

- [ ] **Step 3: Update docker-compose.yml and local docs**

Edit `docker-compose.yml` so it mirrors CI-backed services as closely as practical:

- update service images to match current CI choices or current compatible replacements where CI uses floating tags;
- align service ports and env vars used by tests;
- keep locally useful services such as Kafka or Redis only if they remain relevant to current tests;
- remove clearly stale or misleading config if it no longer reflects actual test execution.

Update `README.md` if it still claims the framework is only for `.NET 8` or if local test setup instructions need a brief refresh.

- [ ] **Step 4: Run full verification commands**

Run:

```bash
dotnet restore Sitko.Core.sln && dotnet build Sitko.Core.sln -c Release --no-restore && dotnet test Sitko.Core.sln --framework net8.0 --no-build && dotnet test Sitko.Core.sln --framework net10.0 --no-build && dotnet pack Sitko.Core.sln -c Release --no-build -p:PackageOutputPath=$(pwd)/packages
```

Expected: restore, build, both test passes, and pack all succeed from the .NET 10 SDK baseline.

- [ ] **Step 5: Commit the integration environment and docs update**

Run:

```bash
git add docker-compose.yml README.md
git commit -m "feat!: align local test services with net10 CI migration"
```

## Self-Review

### Spec coverage

- `.NET 10 SDK baseline` is covered by Task 1 and Task 4.
- `net10.0` library targets with `net8.0` retained are covered by Task 1.
- `package upgrades and conditional versions` are covered by Task 2.
- `demo applications to net10.0` are covered by Task 3.
- `CI tests for both runtimes` are covered by Task 4.
- `docker-compose alignment with CI` is covered by Task 5.
- `pack/build verification` is covered by Task 5.

### Placeholder scan

- No `TODO`/`TBD` placeholders remain.
- Every task lists concrete files and concrete commands.
- The only intentionally open-ended part is fixing compile errors reported by the migration build, which is unavoidable because the exact error list depends on package and framework interactions discovered during execution.

### Consistency check

- Libraries are consistently described as `net8.0;net10.0`.
- Demo/perf apps are consistently described as `net10.0`.
- CI consistently uses explicit `--framework net8.0` and `--framework net10.0` runs.
