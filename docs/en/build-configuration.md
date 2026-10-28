# Build and dependency configuration

[Русский](../ru/build-configuration.md)

The [root Directory.Build.props](../../Directory.Build.props) scopes common settings
to project directories. The local [src](../../src/Directory.Build.props),
[tests](../../tests/Directory.Build.props), and [apps](../../apps/Directory.Build.props)
files import it first, then apply local settings. MSBuild automatically finds only
the nearest file.

| Area | Common settings | Local settings |
| --- | --- | --- |
| `src` | `net10.0`, `LangVersion=14.0`, nullable, implicit usings, warnings | Packaging, package metadata, SourceLink, license and icon |
| `tests` | The same target framework and language, nullable, implicit usings, warnings | `IsPackable=false`, reference to `Sitko.Core.Xunit`, `GitHubActionsTestLogger` |
| `apps` | Nullable, implicit usings, warnings | Single TFM `net10.0` and application settings |
| `perf` | No common property overrides | Single TFM `net10.0`, `OutputType=Exe`, `IsPackable=false`, `DisableAppCheck=true` |

All direct package dependency versions are declared in the
[root Directory.Packages.props](../../Directory.Packages.props).
Projects use `PackageReference` without `Version`. A `PackageVersion` declaration
does not add a reference or introduce test dependencies into libraries.
`PrivateAssets` and `IncludeAssets` remain on the corresponding reference.

All projects target only `net10.0`. The stable SDK is selected by
[global.json](../../global.json): `10.0.403`, `rollForward=latestFeature`,
`allowPrerelease=false`. This permits newer stable SDK 10.0 feature bands,
but does not switch the build to SDK 11.

Microsoft packages use current compatible stable versions; different package
families need not share the same version number. The PostgreSQL test obtains
its shared `Microsoft.EntityFrameworkCore.Design` version without `VersionOverride`.

Packaging paths for the license and icon are anchored to `MSBuildThisFileDirectory`.
Protobuf, resource, and copied-file paths remain in their projects.

Validate shared changes by evaluating all affected projects, building the solution,
and packing without publishing. Check package metadata, package contents, and the
scope of settings, including `apps` and `perf`.

## AntDesign build

`Sitko.Core.Blazor.AntDesign` builds and packs using standard .NET SDK commands.
The module requires no Node.js setup or npm commands. Its components use styles
from `AntDesign 0.20.4`; the package contains no additional Sitko.Core frontend
assets for these components.
See the [migration guide](../migration-v13.en.md) for application changes.

References: [MSBuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory),
[NuGet CPM](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
Contract: [specification](../../specs/20261028-080000-build-configuration/spec.md).
