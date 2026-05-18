# Sitko.Core Sitko.FluentValidation 2.0.0 Upgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update `Sitko.Core` to `Sitko.FluentValidation 2.0.0` and verify the known validation-sensitive test paths on `net8.0` and `net10.0`.

**Architecture:** Make a single central package version change and verify the previously risky repository validation flows first. Only widen verification if the targeted suites expose a broader dependency integration regression.

**Tech Stack:** .NET 8, .NET 10, Central Package Management, FluentValidation 12, Sitko.FluentValidation 2.0.0, xUnit

---

### Task 1: Bump Package and Run Targeted Verification

**Files:**
- Modify: `src/Directory.Packages.props`
- Test: `tests/Sitko.Core.Repository.Remote.Tests/Sitko.Core.Repository.Remote.Tests.csproj`
- Test: `tests/Sitko.Core.Repository.EntityFrameworkCore.Tests/Sitko.Core.Repository.EntityFrameworkCore.Tests.csproj`
- Optional test: `tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj`

- [ ] **Step 1: Update the central package version**

In `src/Directory.Packages.props`, change:

```xml
<PackageVersion Include="Sitko.FluentValidation" Version="1.12.0" />
```

to:

```xml
<PackageVersion Include="Sitko.FluentValidation" Version="2.0.0" />
```

- [ ] **Step 2: Run remote repository tests on `net8.0`**

Run:

```bash
TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net8.0 "tests/Sitko.Core.Repository.Remote.Tests/Sitko.Core.Repository.Remote.Tests.csproj"
```

Expected: PASS.

- [ ] **Step 3: Run remote repository tests on `net10.0`**

Run:

```bash
TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net10.0 "tests/Sitko.Core.Repository.Remote.Tests/Sitko.Core.Repository.Remote.Tests.csproj"
```

Expected: PASS.

- [ ] **Step 4: Run EF repository tests on `net8.0`**

Run:

```bash
TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net8.0 "tests/Sitko.Core.Repository.EntityFrameworkCore.Tests/Sitko.Core.Repository.EntityFrameworkCore.Tests.csproj"
```

Expected: PASS.

- [ ] **Step 5: Run EF repository tests on `net10.0`**

Run:

```bash
TESTS__USEPOSTGRES=true DB__POSTGRES__TESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__SECONDTESTDBCONTEXT__HOST=127.0.0.1 DB__POSTGRES__TPHDBCONTEXT__HOST=127.0.0.1 dotnet test --logger GitHubActions --framework net10.0 "tests/Sitko.Core.Repository.EntityFrameworkCore.Tests/Sitko.Core.Repository.EntityFrameworkCore.Tests.csproj"
```

Expected: PASS.

- [ ] **Step 6: Only if needed, widen verification to app tests**

If any of the previous failures point to general DI/configuration behavior rather than repository-specific validation, run:

```bash
dotnet test --logger GitHubActions --framework net8.0 "tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj"
dotnet test --logger GitHubActions --framework net10.0 "tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj"
```

Expected: PASS.

- [ ] **Step 7: Commit the package integration update**

Use a non-breaking dependency-style commit because the breaking change is in the upstream package, not in this repo’s public API shape.

```bash
git add src/Directory.Packages.props
git commit -m "chore: update sitko fluentvalidation to 2.0.0"
```
