# Build and dependency configuration

[Русский](../ru/build-configuration.md)

The [root Directory.Build.props](../../Directory.Build.props) scopes common settings
to project directories. The local [src](../../src/Directory.Build.props),
[tests](../../tests/Directory.Build.props), and [apps](../../apps/Directory.Build.props)
files import it first, then apply local settings. MSBuild automatically finds only
the nearest file.

| Area | Common settings | Local settings |
| --- | --- | --- |
| `src` | `net8.0;net10.0`, `LangVersion=latest`, nullable, implicit usings, warnings | Packaging, package metadata, SourceLink, license and icon |
| `tests` | The same TFMs and language, nullable, implicit usings, warnings | `IsPackable=false`, reference to `Sitko.Core.Xunit`, `GitHubActionsTestLogger` |
| `apps` | Nullable, implicit usings, warnings | Single TFM `net10.0` and application settings |
| `perf` | No common property overrides | Single TFM `net10.0`, `OutputType=Exe`, `IsPackable=false`, `DisableAppCheck=true` |

All direct package dependency versions are declared in the
[root Directory.Packages.props](../../Directory.Packages.props).
Projects use `PackageReference` without `Version`. A `PackageVersion` declaration
does not add a reference or introduce test dependencies into libraries.
`PrivateAssets` and `IncludeAssets` remain on the corresponding reference.

Microsoft and EF Core package versions depend on `TargetFramework`.
The PostgreSQL test uses the shared `Microsoft.EntityFrameworkCore.Design` version:
`9.0.6` for `net8.0` and `10.0.8` for `net10.0`, without `VersionOverride`.
Adding a target framework requires matching entries in the version table.

Packaging paths for the license and icon are anchored to `MSBuildThisFileDirectory`.
Protobuf, resource, and copied-file paths remain in their projects.

Validate shared changes by evaluating all affected projects, building the solution,
and packing without publishing. Check package metadata, package contents, and the
scope of settings, including `apps` and `perf`.

References: [MSBuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory),
[NuGet CPM](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
Contract: [specification](../../specs/20261028-080000-build-configuration/spec.md).
