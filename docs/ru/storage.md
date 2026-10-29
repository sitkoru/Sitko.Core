# Файлы, метаданные и изображения

[English](../en/storage.md) · [Все темы](README.md)

`Sitko.Core.Storage` задаёт `IStorage` и типизированный `IStorage<TOptions>`.
Транспорт и провайдер метаданных выбираются отдельно. После
[регистрации Sitko.Core](application.md) подключите нужную реализацию.

## Локальный файл

Пример для консольного проекта со ссылкой на `Sitko.Core.Storage.FileSystem`:

```csharp
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage;
using Sitko.Core.Storage.FileSystem;

var builder = Host.CreateApplicationBuilder(args);
var core = builder.AddSitkoCore();
core.AddFileSystemStorage<GuideStorageOptions>();
core.AddFileSystemStorageMetadata<GuideStorageOptions>();
using var host = builder.Build();
await host.StartAsync();
var storage = host.Services.GetRequiredService<IStorage<GuideStorageOptions>>();
await using var source = new MemoryStream(Encoding.UTF8.GetBytes("Hello"));
var item = await storage.SaveAsync(source, "hello.txt", "example");
await using (var download = await storage.DownloadAsync(item.FilePath))
{
    if (download is null) throw new InvalidOperationException("File was not found");
    using var reader = new StreamReader(download.Stream);
    Console.WriteLine(await reader.ReadToEndAsync());
}
await storage.DeleteAsync(item.FilePath);
await host.StopAsync();

public class GuideStorageOptions : StorageOptions, IFileSystemStorageOptions
{
    public string StoragePath { get; set; } =
        Path.Combine(Path.GetTempPath(), "sitko-core-storage-guide");
}
```

Ожидается `Hello`; пример создаёт файл и его метаданные в отдельном временном
каталоге и удаляет созданный файл. Пользователю процесса нужны права записи/чтения.
Входной поток должен поддерживать `Seek`: общий `SaveAsync` переводит его в начало.
Поток скачивания принадлежит `DownloadResult`; освобождайте его через `using`.
`GetAsync`/`DownloadAsync` могут вернуть `null` для отсутствующего файла; ошибки
доступа не следует считать отсутствием.

## Выбор реализации

| Пакет (префикс `Sitko.Core.`) | Подключение и конфигурация |
| --- | --- |
| `Storage.FileSystem` | `AddFileSystemStorage<TOptions>()`; `TOptions : StorageOptions, IFileSystemStorageOptions`; `StoragePath`; секции `Storage:FileSystem:Default` и `Storage:FileSystem:<тип>`. Метаданные: `AddFileSystemStorageMetadata<TOptions>()`. |
| `Storage.S3` | `AddS3Storage<TOptions>()`; наследник `S3StorageOptions`; `Server`, `Bucket`, `AccessKey`, `SecretKey`, `Region`; секции `Storage:S3:Default` и `Storage:S3:<тип>`. Метаданные S3: `AddS3StorageMetadata<TOptions>()`. |
| `Storage.Metadata.Postgres` | `AddPostgresStorageMetadata<TOptions>()`; секции `Storage:Metadata:Postgres:Default` и `Storage:Metadata:Postgres:<тип>`; helper также регистрирует `StorageDbContext`. |
| `Storage.Remote` | `AddRemoteStorage<TOptions>()`; `TOptions` реализует `IRemoteStorageOptions` с `RemoteUrl`, `HttpClientFactory`; секции `Storage:Remote:Default` и `Storage:Remote:<тип>`. |
| `Storage.Remote.Server` | Базовый `BaseRemoteStorageController<TOptions,TMetadata>` для собственного HTTP-контроллера; маршруты и доступ задаёт приложение. |

Для S3 заранее настройте endpoint и права bucket. Предподписанные ссылки выключены
по умолчанию (`GeneratePreSignedUrls=false`); срок при включении — один час.
`PublicUri` формирует ссылку, но не настраивает HTTP-раздачу или доступ к bucket.
`DeleteBucketOnCleanup=false`; операции очистки проверяйте только на выделенном
хранилище. Пример реального S3-стенда: [тесты](../../tests/Sitko.Core.Storage.S3.Tests).

`Prefix`, `Name`, `IsDefault`, `PreserveOriginalFileName` задаются в `StorageOptions`.
Имя файла по умолчанию заменяется GUID с расширением; выбирайте входные имена
с расширением. Для нескольких хранилищ предпочтителен `IStorage<TOptions>`.
Нетипизированный `IStorage` выбирает единственное `IsDefault=true`, иначе последнее;
несколько значений `IsDefault=true` дают ошибку.

Без провайдера метаданных недоступны изменение метаданных и дерево каталогов.
При повторном вызове `AddPostgresStorageMetadata<TOptions>()` для разных хранилищ
в одном Host helper повторно регистрирует один `StorageDbContext`-модуль и может
завершиться ошибкой. Рабочий пример одной регистрации БД с отдельными metadata-модулями:
[проверки изоляции](../../tests/Sitko.Core.Storage.Metadata.Postgres.Tests).
Кэширование опционально; успешное чтение из кэша не проверяет права на исходном сервере.

## Изображения

`Sitko.Core.ImgProxy` генерирует URL через `AddImgProxy(...)` и
`IImgProxyUrlGenerator`. Секция `ImgProxy`: `Host`, `Key`, `Salt`, `EncodeUrls`,
`DisableProxy`. Это генератор ссылок, сам imgproxy разворачивается отдельно.
`Sitko.Core.Storage.ImgProxy` добавляет `AddImgProxyStorage<TOptions>()` и
`IImgProxyUrlGenerator<TOptions>` для `StorageItem`; нужны модули Storage и ImgProxy.
Проверьте реальный запрос сгенерированного URL и доступ imgproxy к исходному файлу.

Источники: [интерфейс](../../src/Sitko.Core.Storage/IStorage.cs),
[общая реализация](../../src/Sitko.Core.Storage/Storage.cs),
[выбор хранилища](../../src/Sitko.Core.Storage/StorageModule.cs),
[S3](../../src/Sitko.Core.Storage.S3/S3StorageModule.cs),
[PostgreSQL metadata](../../src/Sitko.Core.Storage.Metadata.Postgres/ApplicationExtensions.cs),
[ImgProxy](../../src/Sitko.Core.ImgProxy/IImgProxyUrlGenerator.cs).
