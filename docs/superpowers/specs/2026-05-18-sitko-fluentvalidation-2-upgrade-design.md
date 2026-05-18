# Sitko.Core Sitko.FluentValidation 2.0.0 Upgrade Design

## Goal

Upgrade `Sitko.Core` from `Sitko.FluentValidation` `1.12.0` to `2.0.0` and verify that the previously identified validation integration paths behave correctly on `net8.0` and `net10.0`.

## Scope

This is a focused package integration change inside `Sitko.Core`.

Planned work:

- update the central package version in `src/Directory.Packages.props`
- keep `FluentValidation.DependencyInjectionExtensions` as-is at `12.0.0`
- run targeted validation-sensitive test suites that cover the known risk surface

Out of scope:

- unrelated package refreshes
- cleanup of unrelated local worktree changes
- broad CI reruns unless targeted verification fails or exposes a wider regression

## Recommended Approach

Use the smallest possible integration step:

1. bump `Sitko.FluentValidation` to `2.0.0`
2. verify remote repository validation behavior on `net8.0` and `net10.0`
3. verify EF repository tests on `net8.0` and `net10.0`
4. only widen verification if those suites expose an app-level or broader validation regression

## Files

### `src/Directory.Packages.props`

Change:

- `Sitko.FluentValidation` `1.12.0` -> `2.0.0`

### Existing local test changes

Do not revert or rewrite current local modifications in:

- `tests/Sitko.Core.Repository.Remote.Tests/Data/RemoteRepositoryTestScope.cs`
- `tests/Sitko.Core.Repository.Remote.Tests/RemoteRepositoryTests.cs`

These files are already part of the active local investigation context and should only be touched further if verification requires it.

## Verification Strategy

Primary targeted verification:

- `tests/Sitko.Core.Repository.Remote.Tests` on `net8.0`
- `tests/Sitko.Core.Repository.Remote.Tests` on `net10.0`
- `tests/Sitko.Core.Repository.EntityFrameworkCore.Tests` on `net8.0`
- `tests/Sitko.Core.Repository.EntityFrameworkCore.Tests` on `net10.0`

Secondary verification only if needed:

- `tests/Sitko.Core.App.Tests` on `net8.0` and `net10.0`

## Success Criteria

The upgrade is successful when:

- `Sitko.FluentValidation` is updated to `2.0.0`
- targeted validation-sensitive test suites pass on both supported TFMs
- no additional workaround is required beyond the already present local test-state context
