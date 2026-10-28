# Настройки сборки и зависимостей

[English](../en/build-configuration.md)

[Корневой Directory.Build.props](../../Directory.Build.props) содержит общие
настройки с явной областью действия по каталогам. Локальные файлы
[src](../../src/Directory.Build.props), [tests](../../tests/Directory.Build.props)
и [apps](../../apps/Directory.Build.props) сначала импортируют корневой файл,
затем добавляют свои настройки. MSBuild автоматически находит только ближайший файл.

| Область | Общие настройки | Локальные настройки |
| --- | --- | --- |
| `src` | `net10.0`, `LangVersion=14.0`, nullable, implicit usings, предупреждения | Упаковка, метаданные пакетов, SourceLink, лицензия и иконка |
| `tests` | Те же платформа и язык, nullable, implicit usings, предупреждения | `IsPackable=false`, ссылка на `Sitko.Core.Xunit`, `GitHubActionsTestLogger` |
| `apps` | Nullable, implicit usings, предупреждения | Один TFM `net10.0` и настройки конкретного приложения |
| `perf` | Без общих переопределений свойств | Один TFM `net10.0`, `OutputType=Exe`, `IsPackable=false`, `DisableAppCheck=true` |

Версии всех прямых пакетных зависимостей задаются в
[корневом Directory.Packages.props](../../Directory.Packages.props).
Проект подключает пакет через `PackageReference` без `Version`. Само объявление
`PackageVersion` не подключает пакет и не переносит тестовые зависимости в библиотеки.
Метаданные `PrivateAssets` и `IncludeAssets` остаются у соответствующей ссылки.

Все проекты используют единственную платформу `net10.0`. Стабильный SDK задаётся
в [global.json](../../global.json): `10.0.403`, `rollForward=latestFeature`,
`allowPrerelease=false`. Этот выбор допускает более новый стабильный набор
компонентов SDK 10.0, но не переключает сборку на SDK 11.

Пакеты Microsoft используют актуальные совместимые стабильные версии; разные
семейства не обязаны иметь одинаковый номер. `Microsoft.EntityFrameworkCore.Design`
для PostgreSQL-теста получает общую версию без `VersionOverride`.

Ссылки упаковки на лицензию и иконку привязаны к `MSBuildThisFileDirectory`.
Пути protobuf, ресурсов и копируемых файлов остаются в проектах.

Изменение общих файлов проверяют вычислением настроек всех затронутых проектов,
сборкой решения и упаковкой без публикации. Проверяют сохранение метаданных,
состава пакетов и области действия настроек, включая `apps` и `perf`.

Справка: [MSBuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory),
[NuGet CPM](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
Контракт: [спецификация](../../specs/20261028-080000-build-configuration/spec.md).
