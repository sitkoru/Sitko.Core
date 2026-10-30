# Документация Sitko.Core 13

[English](../en/README.md)

Sitko.Core — модульная основа приложений на .NET 10. Подключайте возможности,
которые нужны приложению; инфраструктурные серверы и прикладные модели
подготавливаются отдельно. Эта документация описывает ветку версии 13;
наличие материала в Git не означает публикацию соответствующих пакетов NuGet.

## Начало работы

1. Проверьте [SDK и сборку](build-configuration.md).
2. Подключите [приложение и модули](application.md).
3. Выберите возможности из каталога ниже и настройте их зависимости.
4. Для обновления приложения изучите [переход с версии 12](migration-v13.md).

[Blazor-демо](blazor-demo.md) показывает формы, URL и файлы без внешней БД.
[gRPC/SD](grpc-service-discovery.md) содержит регистрации, параметры и требования
к TLS/Consul. [gRPC-демо](grpc-demo.md) выполняет RPC через локальный Consul.
[Тестовая оснастка](testing.md) описывает проверки и ограничения стендов.

## Каталог возможностей

У пакетов в таблице общий префикс `Sitko.Core.`. Каждая строка ведёт к руководству
по сценарию; базовые контракты и реализации объединены в связанной теме.

| Возможность | Пакеты |
| --- | --- |
| [Приложение](application.md) | `App`, `App.Web` |
| [Vault](vault.md) | `Configuration.Vault` |
| [Логи, метрики, трассировка](observability.md) | Входит в `App` |
| [Вход и проверка JWT](oidc-authentication.md) | `Auth`, `Auth.OIDC` |
| [Локальные учётные записи](identity.md) | `Identity` |
| [Redis и Valkey](redis-sentinel.md) | `Redis` |
| [Базы и репозитории](data.md) | `Db`, `Db.InMemory`, `Db.Postgres`, `Repository`, `Repository.EntityFrameworkCore`, `Repository.Grpc`, `Repository.Search`, `ClickHouse`, `ClickHouse.Linq2db`, `Search`, `Search.OpenSearch` |
| [Очереди и задачи](messaging.md) | `Queue`, `Queue.InMemory`, `Queue.Kafka`, `Kafka`, `Tasks`, `Tasks.Kafka` |
| [Файлы и изображения](storage.md) | `Storage`, `Storage.FileSystem`, `Storage.S3`, `Storage.Metadata.Postgres`, `Storage.Remote`, `Storage.Remote.Server`, `Storage.ImgProxy`, `ImgProxy` |
| [Почта](email.md) | `Email`, `Email.Smtp` |
| [gRPC и обнаружение сервисов](grpc-service-discovery.md) | `Grpc`, `Grpc.Client`, `Grpc.Server`, `ServiceDiscovery`, `ServiceDiscovery.Consul` |
| [Blazor](blazor.md) | `Blazor`, `Blazor.Server`, `Blazor.Wasm`, `Blazor.AntDesign`, `Blazor.AntDesign.Server`, `Blazor.MudBlazor`, `Blazor.MudBlazor.Server`, `Blazor.FluentValidation`, `Blazor.FileUpload` |
| [PDF и снимки](pdf.md) | `Pdf` |
| [Тестовая оснастка](testing.md) | `Xunit`, `Xunit.Web` |

## Материалы разработки

[Настройки сборки](build-configuration.md) · [Запуск тестов](testing.md) ·
[Инструкции участникам](../contributing.md) · [Правила документации](../documentation-guidelines.md).
Спецификации и материалы процесса сопровождаются в своём установленном формате;
пользовательские руководства имеют пары RU/EN.
