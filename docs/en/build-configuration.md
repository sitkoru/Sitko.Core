# Build and dependency configuration

[Русский](../ru/build-configuration.md)

## Solution and tooling

The only solution file is [Sitko.Core.slnx](../../Sitko.Core.slnx).
It includes libraries, tests, examples, and the benchmark project.
The solution uses the default Debug/Release configurations and Any CPU platform.
Explicit x64/x86 solution configurations are not defined.

Build and pack commands from the root automatically select the only solution:
`dotnet build -c Release` and `dotnet pack -c Release --no-build`.
Commands naming the solution explicitly use `Sitko.Core.slnx`.
Project-based test commands do not depend on the solution format.

The SDK selected by global.json supports SLNX. Use a Rider version supporting both .NET 10 and SLNX
or Visual Studio 2026 (18.0) or later. Shared JetBrains settings remain in
[Sitko.Core.sln.DotSettings](../../Sitko.Core.sln.DotSettings); this is not
a second solution file. A parallel SLN solution is not maintained.

The [root Directory.Build.props](../../Directory.Build.props) defines common settings
for all projects, including applications and benchmarks. The local [src](../../src/Directory.Build.props),
[tests](../../tests/Directory.Build.props), and [apps](../../apps/Directory.Build.props)
files import it first, then apply local settings. MSBuild automatically finds only
the nearest file.

| Area | Common settings | Local settings |
| --- | --- | --- |
| `src` | `net10.0`, `LangVersion=14.0`, nullable, implicit usings, warnings | Packaging, package metadata, SourceLink, license and icon |
| `tests` | The same target framework and language, nullable, implicit usings, warnings | `IsPackable=false`, reference to `Sitko.Core.Xunit`; runner and reports only for executable tests |
| `apps` | The same target framework and language, nullable, implicit usings, warnings | Application settings |
| `perf` | The same target framework and language, nullable, implicit usings, warnings | `OutputType=Exe`, `IsPackable=false`, `DisableAppCheck=true` |

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
See the [migration guide](migration-v13.md) for application changes.

References: [MSBuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory),
[NuGet CPM](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
Contract: [specification](../../specs/20261028-080000-build-configuration/spec.md).

[Running tests and migrating infrastructure consumers](testing.md).
