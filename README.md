# Sitko.Core

Opinionated framework on top of .NET 8 and .NET 10 with ASP.NET Core for building microservices.

Provides base application and modules for fast application building.

# Modules

- Logging
- Tracing
- Service discovery
- Database access
- Search
- Queue 
- GRPC
- Storage
- Email 
- Unit testing

# Using

See [demo app](apps/Blazor/Sitko.Core.Apps.Blazor) for now


Общее подключение `Sitko.Core.Redis`, настройки аутентификации и запуск тестов: [Redis, Valkey Sentinel и Cluster](docs/redis-sentinel.md).

## Спецификации

В репозитории подключён [GitHub Spec Kit](https://github.com/github/spec-kit)
версии `1.1.2` с интеграцией Codex и сценариями на Python. Открывай в Codex
корень репозитория или его рабочего дерева: навыки находятся в `.agents/skills/`,
а шаблоны, сценарии и настройки — в `.specify/`. Эти файлы хранятся в Git;
повторная инициализация после клонирования не требуется.

Конституция `.specify/memory/constitution.md` пока содержит штатный шаблон.
Сначала согласуем принципы через `$speckit-constitution`, затем оформляем
требования через `$speckit-specify`, план через `$speckit-plan` и задачи через
`$speckit-tasks`. Реализация запускается отдельно через `$speckit-implement`.
Текущая задача выбирается локально: `.specify/feature.json` исключён из Git.

Для служебных команд нужен Specify CLI; [инструкция установки](https://github.com/github/spec-kit#quick-start).
Команда `specify check` проверяет доступность инструментов в окружении.
