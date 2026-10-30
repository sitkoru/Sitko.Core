# Базы данных, репозитории и поиск

[English](../en/data.md) · [Все темы](README.md)

Модули подключаются после [регистрации Sitko.Core](application.md).
Типы `AppDbContext`, сущности и репозитории в описании — типы приложения.

## Entity Framework Core

`Sitko.Core.Db` задаёт `IDbContextProvider<TDbContext>` и общие параметры.
Подключите `Sitko.Core.Db.Postgres` и вызовите
`AddPostgresDatabase<AppDbContext>()` для своего `DbContext`.
Настройки сначала читаются из `Db:Postgres:Default`, затем
`Db:Postgres:AppDbContext`. Доступны `Host` (`localhost`), `Port` (`5432`),
`Username` (`postgres`), `Password`, `Database` (`dbname`), `Schema`, `SslMode`
(`Disable`) и `ConnectionOptions`. Для реального окружения выберите подходящий
режим TLS и передайте секреты извне. При `VerifyCA`/`VerifyFull` используется
`SaslCertBase64` как корневой сертификат.

`AutoApplyMigrations=true` применяет ожидающие миграции при старте. Если миграциями
управляет отдельный процесс, задайте `false`; регистрацию модуля не следует
использовать как пробный запуск против рабочей БД. `MaxMigrationsApplyTryCount=1`.
Контекст и соединения по умолчанию используют пулы; `EnableContextPooling` и
`EnableNpgsqlPooling` управляют ими отдельно. `EnableSensitiveLogging=false`.

`Sitko.Core.Db.InMemory` подключается через `AddInMemoryDatabase<AppDbContext>()`;
секции — `Db:InMemory:Default` и `Db:InMemory:AppDbContext`. Это EF InMemory,
не модель транзакций PostgreSQL: предупреждение об игнорировании транзакций подавлено.

`Sitko.Core.Repository` содержит `IRepository`/`IRepositoryQuery`, сущности,
фильтры и результаты операций. `Sitko.Core.Repository.EntityFrameworkCore`
связывает их с EF: `AddEFRepositories<TAssembly>()` сканирует сборку указанного
типа; секция — `Repositories:EF`. Реализуйте репозиторий своей сущности и используйте
его через DI; проверяйте результат изменения до продолжения работы.
Начальный образец — [EF-проверки](../../tests/Sitko.Core.Repository.EntityFrameworkCore.Tests).

Проверка интеграции: сохраните сущность, прочитайте её из нового scope, измените
и удалите; отдельно проверьте откат транзакции на настоящем PostgreSQL.
`Sitko.Core.Repository.Grpc` содержит преобразования `ApiRequestInfo` в запросы
репозитория (`ApplyRequestParams`) и конвертеры типов. Он не публикует произвольный
репозиторий автоматически. Ограничения фильтрации и доступ к данным задаёт сервер
приложения; конфигурация транспорта — в [gRPC/SD](grpc-service-discovery.md).

## ClickHouse

`Sitko.Core.ClickHouse`: `AddClickHouse(...)`, секция `ClickHouse`, параметры
`Host=localhost`, `Port=8123`, `UserName=clickhouse`, обязательный непустой `Database`,
`Password`, `WithSsl=false`, `Settings`, `MaxConnectionsPerServer=1`.
Для HTTP(S)-подключения используйте соответствующий порт; проверку сертификатов
по умолчанию не отключают (`DisableCertificatesValidation=false`).
`Sitko.Core.ClickHouse.Linq2db`: `AddClickHouseDb<TDbContext>()` для наследника
`BaseClickHouseDbContext`, секции `ClickHouse:Db:Default` и `ClickHouse:Db:<имя типа>`.
Он требует зарегистрированного `ClickHouseModule`.

Проверьте чтение и вставку в отдельной тестовой базе; примеры окружения и запросов:
[ADO](../../tests/Sitko.Core.ClickHouse.Tests),
[LINQ](../../tests/Sitko.Core.ClickHouse.Linq2db.Tests).

## Поиск OpenSearch

`Sitko.Core.Search` задаёт модели, провайдеры и `ISearcher<T>`.
Подключите `Sitko.Core.Search.OpenSearch`, вызовите `AddOpenSearch(...)` и
зарегистрируйте свой провайдер через `RegisterSearchProvider<...>()`.
Секция `Search:OpenSearch` содержит `Url` (`http://localhost:9200`), `Login`,
`Password`, `Prefix`, `Refresh` (`False`), `DisableCertificatesValidation` (`false`)
и параметры подсветки/стеммера. Это поисковый модуль, а не sink логирования.
`Sitko.Core.Repository.Search` подключается через `AddSearchRepository()` и
связывает репозиторий с поисковым провайдером.

Инициализация поисковых провайдеров выполняется в фоне, ошибки пишутся в журнал;
успешный старт Host не означает, что индекс готов. При `Refresh=False` запись
не обязана немедленно находиться поиском. На отдельном индексе проверьте
индексацию, поиск и переиндексацию с несколькими страницами данных.
Образцы: [OpenSearch](../../tests/Sitko.Core.Search.OpenSearch.Tests).

Источники: [PostgreSQL](../../src/Sitko.Core.Db.Postgres/PostgresDatabaseModule.cs),
[параметры PostgreSQL](../../src/Sitko.Core.Db.Postgres/PostgresDatabaseModuleConfig.cs),
[EF](../../src/Sitko.Core.Repository.EntityFrameworkCore/EFRepositoriesModule.cs),
[ClickHouse](../../src/Sitko.Core.ClickHouse/ClickHouseModuleOptions.cs),
[поиск](../../src/Sitko.Core.Search/SearchModule.cs),
[OpenSearch](../../src/Sitko.Core.Search.OpenSearch/OpenSearchModuleOptions.cs).
