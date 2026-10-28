# Настройки сборки и зависимостей

[English](../en/build-configuration.md)

## Решение и инструменты

Единственный файл решения — [Sitko.Core.slnx](../../Sitko.Core.slnx).
Он содержит библиотеки, тесты, примеры и проект измерений производительности.
Конфигурации Debug/Release доступны для Any CPU, x64 и x86; все три платформы
решения направлены на проектную платформу Any CPU.

Сборка и упаковка из корня используют автоматический выбор единственного решения:
`dotnet build -c Release` и `dotnet pack -c Release --no-build`.
Команды с явным именем решения используют `Sitko.Core.slnx`.
Тестовый запуск по отдельным проектам не зависит от формата решения.

SDK из global.json поддерживает SLNX. Для IDE нужен Rider с поддержкой .NET 10 и SLNX
либо Visual Studio 2026 (18.0) и новее. Общий слой настроек JetBrains остаётся в
[Sitko.Core.sln.DotSettings](../../Sitko.Core.sln.DotSettings); его имя не является
вторым файлом решения. Второй SLN рядом с SLNX не сопровождается.

[Корневой Directory.Build.props](../../Directory.Build.props) задаёт общие
настройки для всех проектов, включая приложения и измерения производительности.
Локальные файлы
[src](../../src/Directory.Build.props), [tests](../../tests/Directory.Build.props)
и [apps](../../apps/Directory.Build.props) сначала импортируют корневой файл,
затем добавляют свои настройки. MSBuild автоматически находит только ближайший файл.

| Область | Общие настройки | Локальные настройки |
| --- | --- | --- |
| `src` | `net10.0`, `LangVersion=14.0`, nullable, implicit usings, предупреждения | Упаковка, метаданные пакетов, SourceLink, лицензия и иконка |
| `tests` | Те же платформа и язык, nullable, implicit usings, предупреждения | `IsPackable=false`, ссылка на `Sitko.Core.Xunit`; раннер и отчёты только у исполняемых тестов |
| `apps` | Те же платформа и язык, nullable, implicit usings, предупреждения | Настройки конкретного приложения |
| `perf` | Те же платформа и язык, nullable, implicit usings, предупреждения | `OutputType=Exe`, `IsPackable=false`, `DisableAppCheck=true` |

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

## Сборка AntDesign

`Sitko.Core.Blazor.AntDesign` собирается и упаковывается обычными командами
.NET SDK. Подготовка Node.js и запуск npm для модуля не требуются.
Компоненты используют стили пакета `AntDesign 0.20.4`; дополнительных
фронтенд-ресурсов Sitko.Core для них в пакете нет.
Действия для приложений описаны в [инструкции перехода](../migration-v13.md).

Справка: [MSBuild](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory),
[NuGet CPM](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
Контракт: [спецификация](../../specs/20261028-080000-build-configuration/spec.md).

[Запуск тестов и переход потребителей оснастки](testing.md).
