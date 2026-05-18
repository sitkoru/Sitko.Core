# Package Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aggressively update the approved package families to latest stable versions while preserving `net8.0` support through TFM-specific splits when required.

**Architecture:** The refresh is isolated to `src/Directory.Packages.props` plus the smallest possible source or test fixes required by package-induced breakage. Verification stays aligned with the current CI shape: build and pack on the repo baseline, then run only the CI-backed test suites affected by these package families.

**Tech Stack:** .NET 10 SDK, Central Package Management, OpenTelemetry, gRPC, KafkaFlow, Serilog, xUnit v3, GitHub Actions-style `dotnet test`.

---

## File Map

**Primary package file**
- Modify: `src/Directory.Packages.props`

**Potentially affected source/test files**
- Modify if required: `src/**/*.csproj`
- Modify if required: `src/**/*.cs`
- Modify if required: `tests/**/*.csproj`
- Modify if required: `tests/**/*.cs`

**Verification targets**
- Verify: `tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj`
- Verify: `tests/Sitko.Core.Grpc.Client.Tests/Sitko.Core.Grpc.Client.Tests.csproj`
- Verify: `tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj`
- Verify: `tests/Sitko.Core.Repository.EntityFrameworkCore.Tests/Sitko.Core.Repository.EntityFrameworkCore.Tests.csproj`
- Verify: `tests/Sitko.Core.Repository.Remote.Tests/Sitko.Core.Repository.Remote.Tests.csproj`
- Verify: `tests/Sitko.Core.Pdf.Tests/Sitko.Core.Pdf.Tests.csproj`

### Task 1: Refresh Approved Package Families In CPM

**Files:**
- Modify: `src/Directory.Packages.props`

- [ ] **Step 1: Capture the current package block before edits**

Run:

```bash
git diff -- src/Directory.Packages.props
```

Expected: no current unstaged changes in `src/Directory.Packages.props` before starting the refresh.

- [ ] **Step 2: Discover latest stable versions for the approved families**

Run:

```bash
dotnet package search OpenTelemetry.Exporter.OpenTelemetryProtocol --take 5 && dotnet package search OpenTelemetry.Instrumentation.GrpcNetClient --take 5 && dotnet package search Npgsql.OpenTelemetry --take 5 && dotnet package search Grpc.AspNetCore --take 5 && dotnet package search Google.Protobuf --take 5 && dotnet package search KafkaFlow --take 5 && dotnet package search KafkaFlow.Retry --take 5 && dotnet package search Serilog.Extensions.Logging --take 5 && dotnet package search Serilog.Sinks.OpenTelemetry --take 5 && dotnet package search xunit.v3 --take 5 && dotnet package search xunit.runner.visualstudio --take 5
```

Expected: latest stable candidates are visible for every approved family.

- [ ] **Step 3: Update `src/Directory.Packages.props` for the approved families only**

Edit these groups in place while preserving the existing section order, comments, and formatting style:

- `OpenTelemetry*` and `Npgsql.OpenTelemetry`
- `Grpc.*` and `Google.Protobuf`
- `KafkaFlow*`
- `Serilog*`
- `xunit*`

Rules:

- prefer latest stable;
- if latest stable works for both TFMs, keep a single unconditional `PackageVersion`;
- if latest stable drops or breaks `net8.0`, split it using the existing conditional `ItemGroup Condition=" '$(TargetFramework)' == 'net8.0' "` and `net10.0` pattern.

- [ ] **Step 4: Run restore to validate the package graph**

Run:

```bash
dotnet restore Sitko.Core.sln
```

Expected: restore succeeds; warnings may remain, but package resolution should be valid across both TFMs.

- [ ] **Step 5: Commit only after verification succeeds later**

Do not commit in this task.

### Task 2: Fix Package-Induced Source Breaks Minimally

**Files:**
- Modify if required: exact source or test files reported by build or test failures

- [ ] **Step 1: Run the repo build on the refreshed package set**

Run:

```bash
dotnet build -c Release
```

Expected: either success, or concrete compile/analyzer failures caused by the refreshed packages.

- [ ] **Step 2: For each build break, write or identify the narrow failing verification first**

If the break is compile-time only, use the build itself as the failing verification. If the break is behavioral or test-related, identify the narrowest affected test project and command before editing code.

- [ ] **Step 3: Apply the smallest source-compatible fix**

Keep fixes limited to:

- package API adaptations;
- changed option or extension method names;
- test runner or adapter compatibility updates;
- logging or telemetry wiring changes required by the new versions.

Avoid unrelated refactors.

- [ ] **Step 4: Re-run the failing verification after each fix**

Run the exact failing command again until it passes before moving on.

- [ ] **Step 5: Re-run the full build after all fixes**

Run:

```bash
dotnet build -c Release
```

Expected: build succeeds.

### Task 3: Run Targeted CI-Faithful Verification

**Files:**
- Verify only; no file changes required unless failures force Task 2 fixes

- [ ] **Step 1: Verify pack on the built output**

Run:

```bash
dotnet pack -c Release --no-build -p:PackageOutputPath="$(pwd)/packages"
```

Expected: pack succeeds.

- [ ] **Step 2: Verify app tests for both TFMs**

Run:

```bash
dotnet test --logger GitHubActions --framework net8.0 tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj && dotnet test --logger GitHubActions --framework net10.0 tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj
```

Expected: both runs pass.

- [ ] **Step 3: Verify gRPC client tests for both TFMs**

Run:

```bash
CONSUL__CONSULURI=http://127.0.0.1:8500 dotnet test --logger GitHubActions --framework net8.0 tests/Sitko.Core.Grpc.Client.Tests/Sitko.Core.Grpc.Client.Tests.csproj && CONSUL__CONSULURI=http://127.0.0.1:8500 dotnet test --logger GitHubActions --framework net10.0 tests/Sitko.Core.Grpc.Client.Tests/Sitko.Core.Grpc.Client.Tests.csproj
```

Expected: both runs pass.

- [ ] **Step 4: Verify Kafka tests for both TFMs**

Run:

```bash
dotnet test --logger GitHubActions --framework net8.0 tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj && dotnet test --logger GitHubActions --framework net10.0 tests/Sitko.Core.Queue.Kafka.Tests/Sitko.Core.Queue.Kafka.Tests.csproj
```

Expected: both runs pass.

- [ ] **Step 5: Verify repository EF tests for both TFMs with Postgres env**

Run:

```bash
TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net8.0 tests/Sitko.Core.Repository.EntityFrameworkCore.Tests/Sitko.Core.Repository.EntityFrameworkCore.Tests.csproj && TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net10.0 tests/Sitko.Core.Repository.EntityFrameworkCore.Tests/Sitko.Core.Repository.EntityFrameworkCore.Tests.csproj
```

Expected: both runs pass.

- [ ] **Step 6: Verify remote repository tests for both TFMs with Postgres env**

Run:

```bash
TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net8.0 tests/Sitko.Core.Repository.Remote.Tests/Sitko.Core.Repository.Remote.Tests.csproj && TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net10.0 tests/Sitko.Core.Repository.Remote.Tests/Sitko.Core.Repository.Remote.Tests.csproj
```

Expected: both runs pass.

- [ ] **Step 7: Verify PDF tests for both TFMs**

Run:

```bash
PUPPETEER_EXECUTABLE_PATH="/Applications/Brave Browser.app/Contents/MacOS/Brave Browser" dotnet test --logger GitHubActions --framework net8.0 tests/Sitko.Core.Pdf.Tests/Sitko.Core.Pdf.Tests.csproj && PUPPETEER_EXECUTABLE_PATH="/Applications/Brave Browser.app/Contents/MacOS/Brave Browser" dotnet test --logger GitHubActions --framework net10.0 tests/Sitko.Core.Pdf.Tests/Sitko.Core.Pdf.Tests.csproj
```

Expected: both runs pass.

### Task 4: Create The Separate Package Refresh Commit

**Files:**
- Modify: all verified files changed by Tasks 1-3

- [ ] **Step 1: Inspect the final diff and status**

Run:

```bash
git status --short && git diff
```

Expected: only intended package-refresh changes are present.

- [ ] **Step 2: Stage only the verified package-refresh files**

Run:

```bash
git add src/Directory.Packages.props
```

If Task 2 required source fixes, also stage those exact files explicitly in the same command.

- [ ] **Step 3: Create the commit**

Run:

```bash
git commit -m "chore: refresh telemetry grpc kafka serilog and xunit packages"
```

Expected: commit succeeds.

- [ ] **Step 4: Confirm the worktree is clean except for unrelated files**

Run:

```bash
git status --short
```

Expected: no staged or unstaged package-refresh files remain.
