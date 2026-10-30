# Запуск тестов

[English](../en/testing.md)

Репозиторий использует .NET SDK из [global.json](../../global.json), xUnit `4.0.2`,
Microsoft.Testing.Platform `2.5.2` и AwesomeAssertions `9.6.0`. Версии задаются в
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

`Email.Tests` содержит общие абстрактные проверки для SMTP-наследника и собственные
исполняемые проверки доставки через `DevEmailModule`. `Storage.Tests` содержит общие
абстрактные проверки, собирается как библиотека и отдельно не запускается.
Исполняемые проекты тестов являются приложениями MTP.

`Tasks.Kafka.Tests` проверяет жизненный цикл задач через Kafka и PostgreSQL. Его
[фикстура](../../tests/Sitko.Core.Tasks.Kafka.Tests/KafkaFixture.cs) создаёт собственные
контейнеры и освобождает их после прогона; для запуска требуется доступный Docker.

```sh
dotnet test --project tests/Sitko.Core.Tasks.Kafka.Tests/Sitko.Core.Tasks.Kafka.Tests.csproj --minimum-expected-tests 3
```

Интеграционные проекты требуют своих сервисов: [PDF](pdf.md#проверка-модуля),
[Redis/Valkey](redis-sentinel.md#локальная-проверка) и окружения существующих заданий CI.

## Проверки GitHub Actions

[Main](../../.github/workflows/main.yml) выполняет исполняемые проекты на
`net10.0` в заданиях по подсистемам. Включены конкретные наборы
`App.Web.Tests`, `Grpc.Server.Tests`, `Repository.Grpc.Tests`, `Repository.Tests`
и `Tasks.Kafka.Tests`.
`Email.Smtp.Tests` исключён: существующий сценарий отправляет настоящее письмо
и не имеет изолированного SMTP-стенда.

Каждый запуск требует хотя бы один тест и пишет TRX с диагностикой в
`TestResults/<job>/<project>/net10.0/`; PDF разделяет подкаталоги `cdp` и `local`.
Задания сохраняют доступные файлы и при ошибке в артефакт
`<job>-net10.0-test-results` на три дня. Ошибка сборки может не создать TRX;
диагностика сохраняется, если раннер успел её записать. Ошибки не подавляются:
неуспешный набор блокирует задание и зависимую публикацию.

Main использует образ SDK по digest. PostgreSQL проходит `pg_isready`;
Consul, MinIO и Vault — проверку HTTP с ограниченными повторами.
PDF, OpenSearch и Redis/Valkey
используют существующие проверки готовности. Kafka и ClickHouse подготавливаются
своими тестовыми фикстурами.

Новый запуск PR отменяет устаревший запуск того же PR и workflow. Для push
и выпуска отмена выключена. [CodeQL](../../.github/workflows/codeql-analysis.yml)
проверяет push и PR в `v13`. Условия выпуска Main сохраняются.

Контракт: [проверки изменений](../../specs/20261028-110000-ci-verification/spec.md).

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
