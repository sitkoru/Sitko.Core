# .NET 10 Support Design

**Goal:** Add .NET 10 support across the repository while preserving .NET 8 support for reusable libraries, moving the repo to build on the .NET 10 SDK, updating compatible dependencies, and running CI tests for both target frameworks.

## Current State

- The repository is a set of reusable .NET libraries and demo applications.
- `global.json` is pinned to `.NET SDK 8.0.0`.
- `src/Directory.Build.props` sets a shared `TargetFramework` of `net8.0` for most libraries.
- `tests/Directory.Build.props` sets a shared `TargetFramework` of `net8.0` for most test projects.
- `apps/*` demo applications currently target `net8.0` directly in project files.
- Package versions are centrally managed in `src/Directory.Packages.props`.
- `Directory.Packages.props` already uses `TargetFramework`-conditioned `ItemGroup`s for some packages, so conditional package versioning is an established pattern.
- CI currently uses a shared container image and runs build/test jobs against a single SDK/runtime path.
- `docker-compose.yml` is no longer aligned with the service images and settings used in CI.

## Scope

This change includes:

- Updating the repo to build with the `.NET 10` SDK.
- Multi-targeting reusable libraries to `net8.0;net10.0` wherever technically possible.
- Updating demo applications to `net10.0` only.
- Updating dependencies to the newest version compatible with retained `.NET 8` support.
- Using conditional package versions when a newer dependency version requires dropping `.NET 8` support.
- Updating CI to run tests for both `net8.0` and `net10.0`.
- Aligning local `docker-compose.yml` with the actual service topology used in CI.

This change does not include:

- Dropping `.NET 8` support for reusable libraries.
- Reorganizing CPM file structure beyond what is required for conditional package versions.
- Broad refactoring unrelated to the migration.

## Recommended Approach

The repository should adopt a split strategy based on artifact type:

- Reusable libraries under `src/` should multi-target `net8.0;net10.0`.
- Test projects under `tests/` should multi-target `net8.0;net10.0` so both runtime paths are exercised in CI.
- Demo and sample applications under `apps/` and perf/demo executables should target `net10.0` only.
- The repository should use `.NET 10` SDK for restore, build, pack, and CI orchestration.

This is the smallest change set that preserves the public support promise for `.NET 8` consumers while enabling validation and packaging for `.NET 10`.

## Target Framework Strategy

### Libraries

Libraries that currently inherit `TargetFramework` from `src/Directory.Build.props` should inherit:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Libraries with project-local `TargetFramework` declarations should be reviewed individually:

- If the project is a reusable library, convert it to `TargetFrameworks` with `net8.0;net10.0`.
- If the project is effectively an application/demo host, convert it to `net10.0` only.

The migration should keep the current centralized approach, with shared defaults in `Directory.Build.props` and only project-level overrides where necessary.

### Tests

Tests should be multi-targeted via `tests/Directory.Build.props`:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Any test project that already overrides `TargetFramework` should be aligned with this strategy unless there is a concrete runtime restriction.

This ensures that package resolution, compile-time compatibility, and runtime behavior are all exercised for both supported TFMs.

### Demo Apps and Perf Projects

Demo applications under `apps/` and perf/demo executables should move to:

```xml
<TargetFramework>net10.0</TargetFramework>
```

These projects are owned by the repo, are not reusable distributed libraries, and should represent the current recommended runtime.

## SDK and Build Tooling Strategy

- `global.json` should be updated to a concrete installed `.NET 10` SDK version.
- The repo should restore, build, test, and pack using the `.NET 10` SDK.
- CI workflows should no longer assume `.NET 8` as the controlling SDK.
- Existing solution and package build commands should be preserved where possible to keep release flow stable.

The `.NET 10` SDK will still build `net8.0` targets, allowing a single SDK baseline for the whole repo.

## Package Management Strategy

`src/Directory.Packages.props` remains the single source of truth for package versions. Its current formatting, logical grouping, comments, and overall structure should be preserved.

### Unconditional upgrades

If a package can be upgraded without breaking `net8.0`, it should be upgraded once in the existing unconditional `PackageVersion` entry.

### Conditional upgrades by TFM

If a package can only be upgraded by dropping `.NET 8` support, it should be expressed with two `PackageVersion` entries in `TargetFramework`-conditioned groups:

- one entry for `net8.0` with the latest supported version;
- one entry for `net10.0` with the newer version.

The file should continue using the existing style of conditional `ItemGroup`s instead of introducing a new layout or separate files.

### Priority package families to review

Special attention is required for:

- `Microsoft.Extensions.*`
- `Microsoft.AspNetCore.*`
- `Microsoft.EntityFrameworkCore.*`
- test packages such as `Microsoft.NET.Test.Sdk`, `xunit.*`, and related loggers/adapters
- packages with known runtime- or TFM-sensitive behavior

### Compatibility expectations

After package upgrades, the migration must account for new SDK/runtime behavior and compile-time changes introduced by `.NET 9` and `.NET 10`, especially in:

- ASP.NET Core
- EF Core
- hosting and configuration
- OpenAPI/Swagger-related packages
- BCL/API obsoletions and overload resolution changes

## CI Design

### Main workflow

The main CI workflow should be updated to run under the `.NET 10` SDK and explicitly execute tests for both `net8.0` and `net10.0`.

Preferred implementation:

- keep the current job structure by subsystem (`apps`, `blazor`, `grpc`, `storage`, etc.);
- within each relevant test job, run each test project twice using `--framework net8.0` and `--framework net10.0`, or use a framework matrix where it keeps the workflow readable;
- keep build/pack jobs on the `.NET 10` SDK only, allowing multi-target packing to produce both library TFMs.

The design favors explicitness over heavy matrix abstraction because the current workflow already groups projects by external service dependency and domain area.

### Release workflow

The release workflow should also use the `.NET 10` SDK baseline and continue packing multi-target libraries from the same commands.

The packaging and publishing flow should remain unchanged except where SDK-related versioning or environment assumptions require updates.

## Docker Compose Design

`docker-compose.yml` should be treated as the local counterpart to the CI service topology.

It should be updated to reflect the services actually used by CI test jobs, including the current images and core runtime settings for:

- Postgres
- NATS / NATS Streaming
- MinIO
- Vault
- Consul
- Sonyflake
- OpenSearch
- Kafka
- any other service directly required by current tests

The compose file should prioritize:

- matching CI image families where practical;
- matching service ports and credentials expectations used by tests;
- keeping local startup straightforward for developers running the same tests locally.

If CI and local needs differ, compose should stay close to CI unless a local-only adjustment is required for usability.

## Validation Strategy

The migration is complete only when all of the following are validated on the `.NET 10` SDK:

1. Solution restore succeeds.
2. Solution build succeeds.
3. Test projects run successfully for `net8.0`.
4. Test projects run successfully for `net10.0`.
5. Demo applications build successfully on `net10.0`.
6. Pack succeeds and produces the expected multi-target library artifacts.

Validation should include the external-service-backed test projects using the updated `docker-compose.yml` or equivalent CI services.

## Risks and Mitigations

### Risk: package version divergence becomes noisy

Mitigation:

- only introduce conditional package versions when a real `.NET 8` compatibility boundary exists;
- keep unconditional versions wherever possible.

### Risk: CI becomes harder to read

Mitigation:

- preserve the current job partitioning;
- add explicit dual-framework test runs within those jobs rather than redesigning the whole workflow.

### Risk: compile/runtime breaks from .NET 9/.NET 10 changes

Mitigation:

- review migration-sensitive packages and affected source during the upgrade;
- run both build and runtime-backed tests on both TFMs.

### Risk: local integration environment drifts from CI again

Mitigation:

- align `docker-compose.yml` directly with current CI service usage during this migration;
- treat compose updates as part of the migration rather than a follow-up.

## Implementation Boundaries

The implementation should prefer repository-level defaults and the smallest correct edits:

- centralize TFM changes in shared props files where possible;
- use project-level overrides only where the repository layout requires them;
- preserve CPM formatting and comments;
- avoid unrelated cleanup and refactoring.

## Success Criteria

The migration is successful when:

- the repo builds with the `.NET 10` SDK;
- reusable libraries support both `net8.0` and `net10.0`;
- demo apps target `net10.0`;
- dependencies are upgraded as far as compatibility allows;
- conditional package versions are used only where needed;
- CI runs tests for both target frameworks;
- local compose services reflect the CI-backed test environment.
