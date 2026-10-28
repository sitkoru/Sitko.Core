# Запуск тестов

[English](../en/testing.md)

Репозиторий использует .NET SDK из [global.json](../../global.json), xUnit `4.0.2`,
Microsoft.Testing.Platform `2.5.1` и AwesomeAssertions `9.6.0`. Версии задаются в
[Directory.Packages.props](../../Directory.Packages.props). Команды выполняются из корня.

```sh
dotnet test --project tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --framework net10.0 --minimum-expected-tests 1
dotnet test --project tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --list-tests
dotnet test --project tests/Sitko.Core.App.Tests/Sitko.Core.App.Tests.csproj --filter-class '*ConfigurationTests*' --minimum-expected-tests 1
```

Запуск конкретного метода ограничивается `--filter-method`, признака — `--filter-trait`.
Параметр VSTest `--filter` не используется. Ненулевой код означает ошибку;
успех процесса без ожидаемых сценариев не подтверждает проверку.

Для TRX добавьте `--report-trx --report-trx-filename app.trx --results-directory TestResults/app`.
В GitHub Actions добавляется `--report-github`; расширения подключаются в
[tests/Directory.Build.props](../../tests/Directory.Build.props) только к исполняемым проектам.
Для диагностического журнала используйте `--diagnostic --diagnostic-output-directory TestResults/diagnostics`.
Сбор покрытия не настроен.

`Email.Tests` и `Storage.Tests` содержат общие абстрактные проверки для наследников;
`Tasks.Kafka.Tests` содержит заготовку без конкретных сценариев. Они собираются как
библиотеки и отдельно не запускаются. Остальные проекты тестов являются приложениями MTP.
Интеграционные проекты требуют своих сервисов: [PDF](../pdf.md#проверка-модуля),
[Redis/Valkey](../redis-sentinel.md#локальная-проверка) и окружения существующих заданий CI.

## Переход потребителя оснастки

`Sitko.Core.Xunit` и `Sitko.Core.Xunit.Web` предоставляют классы подготовки окружения
как библиотеки. В собственном исполняемом тестовом проекте подключите
`xunit.v3.mtp-v2` версии `4.0.2`, задайте `OutputType=Exe` и
`UseMicrosoftTestingPlatformRunner=true`. В `global.json` рядом с настройками SDK добавьте:

```json
"test": { "runner": "Microsoft.Testing.Platform" }
```

Удалите ссылки на `Microsoft.NET.Test.Sdk` и `xunit.runner.visualstudio`,
если проект использует только MTP. Для утверждений подключается AwesomeAssertions;
пространства имён `FluentAssertions` заменяются на `AwesomeAssertions`.
`BaseTest`, `BaseTestScope` и `WebTestScope` сохраняют свои API.

Контракт: [спецификация](../../specs/20261028-103000-test-platform/spec.md).
