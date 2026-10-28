# Настройки сборки и зависимостей

[English](../en/build-configuration.md)

[Корневой Directory.Build.props](../../Directory.Build.props) содержит общие
настройки с явной областью действия по каталогам. Локальные файлы
[src](../../src/Directory.Build.props), [tests](../../tests/Directory.Build.props)
и [apps](../../apps/Directory.Build.props) сначала импортируют корневой файл,
затем добавляют свои настройки. MSBuild автоматически находит только ближайший файл.

| Область | Общие настройки | Локальные настройки |
| --- | --- | --- |
| `src` | `net8.0;net10.0`, `LangVersion=latest`, nullable, implicit usings, предупреждения | Упаковка, метаданные пакетов, SourceLink, лицензия и иконка |
| `tests` | Те же TFM и язык, nullable, implicit usings, предупреждения | `IsPackable=false`, ссылка на `Sitko.Core.Xunit`, `GitHubActionsTestLogger` |
| `apps` | Nullable, implicit usings, предупреждения | Один TFM `net10.0` и настройки конкретного приложения |
| `perf` | Без общих переопределений свойств | Один TFM `net10.0`, `OutputType=Exe`, `IsPackable=false`, `DisableAppCheck=true` |

Версии всех прямых пакетных зависимостей задаются в
[корневом Directory.Packages.props](../../Directory.Packages.props).
Проект подключает пакет через `PackageReference` без `Version`. Само объявление
`PackageVersion` не подключает пакет и не переносит тестовые зависимости в библиотеки.
Метаданные `PrivateAssets` и `IncludeAssets` остаются у соответствующей ссылки.

Версии Microsoft-пакетов и EF Core выбираются по `TargetFramework`.
PostgreSQL-тест использует общую версию `Microsoft.EntityFrameworkCore.Design`:
`9.0.6` для `net8.0`, `10.0.8` для `net10.0`, без `VersionOverride`.
При добавлении целевой платформы нужно согласованно задать её версии в таблице.

Ссылки упаковки на лицензию и иконку привязаны к `MSBuildThisFileDirectory`.
Пути protobuf, ресурсов и копируемых файлов остаются в проектах.

Изменение общих файлов проверяют вычислением настроек всех затронутых проектов,
сборкой решения и упаковкой без публикации. Проверяют сохранение метаданных,
состава пакетов и области действия настроек, включая `apps` и `perf`.

Справка: [MSBuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory),
[NuGet CPM](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
Контракт: [спецификация](../../specs/20261028-080000-build-configuration/spec.md).
