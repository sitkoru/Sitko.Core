# Files, metadata and images

[Русский](../ru/storage.md) · [All topics](README.md)

`Sitko.Core.Storage` defines `IStorage` and typed `IStorage<TOptions>`.
Select a transport and metadata provider separately. After
[registering Sitko.Core](application.md), connect the implementation you need.

## Local file

In a console project referencing `Sitko.Core.Storage.FileSystem`:

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

Expected output: `Hello`. The example creates a file and metadata in a dedicated
temporary directory and deletes its file. The process needs read/write permissions.
The input stream must support `Seek`: shared `SaveAsync` rewinds it.
`DownloadResult` owns the downloaded stream; dispose it with `using`.
`GetAsync`/`DownloadAsync` may return `null` for missing files; do not interpret
access failures as missing files.

## Choosing an implementation

| Package (prefix `Sitko.Core.`) | Registration and configuration |
| --- | --- |
| `Storage.FileSystem` | `AddFileSystemStorage<TOptions>()`; `TOptions : StorageOptions, IFileSystemStorageOptions`; `StoragePath`; sections `Storage:FileSystem:Default` and `Storage:FileSystem:<type>`. Metadata: `AddFileSystemStorageMetadata<TOptions>()`. |
| `Storage.S3` | `AddS3Storage<TOptions>()`; subclass `S3StorageOptions`; `Server`, `Bucket`, `AccessKey`, `SecretKey`, `Region`; sections `Storage:S3:Default` and `Storage:S3:<type>`. S3 metadata: `AddS3StorageMetadata<TOptions>()`. |
| `Storage.Metadata.Postgres` | `AddPostgresStorageMetadata<TOptions>()`; sections `Storage:Metadata:Postgres:Default` and `Storage:Metadata:Postgres:<type>`; the helper also registers `StorageDbContext`. |
| `Storage.Remote` | `AddRemoteStorage<TOptions>()`; `TOptions` implements `IRemoteStorageOptions` with `RemoteUrl`, `HttpClientFactory`; sections `Storage:Remote:Default` and `Storage:Remote:<type>`. |
| `Storage.Remote.Server` | Base `BaseRemoteStorageController<TOptions,TMetadata>` for your HTTP controller; the application defines routes and access. |

Configure the S3 endpoint and bucket permissions first. Presigned links are disabled
by default (`GeneratePreSignedUrls=false`); when enabled they expire in one hour.
`PublicUri` builds a link but does not configure HTTP hosting or bucket access.
`DeleteBucketOnCleanup=false`; test cleanup only against dedicated storage.
For a real S3 environment, see the [tests](../../tests/Sitko.Core.Storage.S3.Tests).

`StorageOptions` defines `Prefix`, `Name`, `IsDefault`, `PreserveOriginalFileName`.
By default, a GUID replaces the filename and retains its extension; use input names
with an extension. Prefer `IStorage<TOptions>` for multiple stores. Untyped `IStorage`
selects the single `IsDefault=true` store, or the last store if none is marked;
multiple default stores cause an error.

Metadata updates and directory trees require a metadata provider. Repeated
`AddPostgresStorageMetadata<TOptions>()` calls for different stores in the same Host
register the same `StorageDbContext` module again and may fail. For one database
registration with separate metadata modules, see the
[isolation tests](../../tests/Sitko.Core.Storage.Metadata.Postgres.Tests).
Caching is optional; a cache hit does not verify current permissions on the origin server.

## Images

`Sitko.Core.ImgProxy` generates URLs through `AddImgProxy(...)` and
`IImgProxyUrlGenerator`. Section `ImgProxy` contains `Host`, `Key`, `Salt`,
`EncodeUrls`, `DisableProxy`. It generates links; deploy imgproxy separately.
`Sitko.Core.Storage.ImgProxy` adds `AddImgProxyStorage<TOptions>()` and
`IImgProxyUrlGenerator<TOptions>` for `StorageItem`, requiring Storage and ImgProxy
modules. Verify an actual request to the generated URL and imgproxy access to the source file.

Sources: [interface](../../src/Sitko.Core.Storage/IStorage.cs),
[shared implementation](../../src/Sitko.Core.Storage/Storage.cs),
[store selection](../../src/Sitko.Core.Storage/StorageModule.cs),
[S3](../../src/Sitko.Core.Storage.S3/S3StorageModule.cs),
[PostgreSQL metadata](../../src/Sitko.Core.Storage.Metadata.Postgres/ApplicationExtensions.cs),
[ImgProxy](../../src/Sitko.Core.ImgProxy/IImgProxyUrlGenerator.cs).
