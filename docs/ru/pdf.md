# PDF и снимки страниц

[English](../en/pdf.md)

`Sitko.Core.Pdf` генерирует PDF и снимки страниц из HTML либо URL с помощью `Microsoft.Playwright` и Chromium. Модуль поддерживает локальный запуск установленного браузера и удалённое подключение через Chrome DevTools Protocol (CDP).

## Регистрация и вызов

После регистрации Sitko.Core вызовите `AddPdfRenderer()` и получите `IPdfRenderer` из контейнера зависимостей:

```csharp
builder.AddSitkoCore().AddPdfRenderer();
// После сборки и запуска приложения:
var renderer = host.Services.GetRequiredService<IPdfRenderer>();
var pdf = await renderer.GetPdfByHtmlAsync("<html lang=\"ru\"><body>Счёт № 13</body></html>");
```

Добавьте пространства имён `Sitko.Core.App`, `Sitko.Core.Pdf` и `Microsoft.Extensions.DependencyInjection`. Пример предполагает `HostApplicationBuilder builder` и собранный из него `IHost host`.

| Метод | Результат | Параметры |
| --- | --- | --- |
| `GetPdfByHtmlAsync` / `GetPdfByUrlAsync` | `byte[]` PDF | `PagePdfOptions?`, `TimeSpan? delay` |
| `GetScreenshotByHtmlAsync` / `GetScreenshotByUrlAsync` | `byte[]` снимка | `PageScreenshotOptions?`, `TimeSpan? delay` |

Типы параметров находятся в `Microsoft.Playwright`. При `options == null` PDF печатает фон, снимок имеет формат PNG и охватывает всю страницу. Переданный объект параметров используется как есть; при необходимости явно задайте `PrintBackground`, `FullPage` и `Type`. `delay` добавляет ожидание после загрузки HTML или страницы. Он не является проверкой готовности произвольных асинхронных графиков.

## Настройки браузера

Секция конфигурации — `PdfRenderer`. Настройки также доступны в `AddPdfRenderer(options => ...)`; поддерживаются перегрузки с контекстом приложения и собственным ключом конфигурации.

| Параметр | По умолчанию | Назначение |
| --- | --- | --- |
| `BrowserWsEndpoint` | отсутствует | При заданном адресе подключение через CDP; иначе локальный запуск |
| `ExecutablePath` | отсутствует | Явный путь к локальному Chromium; иначе используется браузер, установленный для версии Playwright |
| `AcceptInsecureCerts` | `false` | Игнорирование ошибок HTTPS в контексте запроса |
| `ViewportSize` | `800 × 600` | Размер окна страницы |
| `BrowserArgs` | `["--no-sandbox"]` | Дополнительные аргументы локального запуска |
| `Headless` | `true` | Локальный запуск без интерфейса |

Пример удалённого браузера:

```json
{
  "PdfRenderer": {
    "BrowserWsEndpoint": "ws://browserless:3000/chromium?token=example"
  }
}
```

Соответствующая переменная окружения: `PdfRenderer__BrowserWsEndpoint`. Адрес, порт, путь и способ авторизации зависят от выбранного CDP-сервера; значение `example` в примере заменяется настройкой окружения.

Каждая операция создаёт собственный контекст. При завершении освобождаются контекст и локальный браузер либо удалённое соединение. Удалённый браузер не завершается рендерером. Ошибки генерации возвращаются как `InvalidOperationException` с исходным исключением в `InnerException`.

## Подготовка окружения

Локальный режим требует заранее установленного Chromium, системных библиотек и шрифтов, поддерживающих текст документа. После сборки проекта установите браузер средствами **той же версии Playwright**, например:

```sh
pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

Путь в примере относится к каталогу сборки приложения; выберите фактическую конфигурацию и целевую платформу. Установку выполняют при подготовке окружения/образа. Кэш браузера должен быть доступен пользователю приложения. Рендерер не скачивает Chromium при запросе. При публикации сохраните поставляемые пакетом файлы драйвера Playwright.

При удалённом подключении локальный Chromium не нужен, но драйвер Playwright нужен приложению. Браузер должен иметь сетевой доступ к URL документа и его CSS, изображениям и шрифтам. `localhost` в HTML обозначает окружение браузера. Проверку совместимости выбранного CDP-сервера выполняйте при обновлении библиотеки или образа браузера.

См. [установку браузеров](https://playwright.dev/dotnet/docs/browsers) и [подключение CDP](https://playwright.dev/dotnet/docs/api/class-browsertype#browser-type-connect-over-cdp).

## Переход на Sitko.Core 13

1. Удалите регистрацию `AddPuppeteer()` и `using Sitko.Core.Puppeteer`, если они использовались только для PDF. Оставьте `AddPdfRenderer()`.
2. Замените `PuppeteerSharp.PdfOptions` на `Microsoft.Playwright.PagePdfOptions`, а `ScreenshotOptions` — на `PageScreenshotOptions`. Поля параметров сопоставьте по API Playwright; это не переименование всех типов один к одному.
3. Перенесите браузерные настройки из `Puppeteer` в `PdfRenderer`. `ViewPortOptions` заменяется `ViewportSize`. `Product` и `Revision` не поддерживаются: для PDF используется Chromium, его версия задаётся установкой окружения. `PUPPETEER_EXECUTABLE_PATH` замените на `PdfRenderer__ExecutablePath` при необходимости явного пути.
4. Подготовьте браузерное окружение и проверьте документы с используемыми шрифтами и ресурсами. Вызовы без дополнительных параметров, например `GetPdfByHtmlAsync(html)`, сохраняют форму после перекомпиляции.

Точки реализации: [интерфейс](../../src/Sitko.Core.Pdf/IPdfRenderer.cs), [рендерер](../../src/Sitko.Core.Pdf/PdfRenderer.cs), [настройки](../../src/Sitko.Core.Pdf/PdfRendererModuleOptions.cs). Контракт: [спецификация](../../specs/20261027-150000-pdf-rendering/spec.md).

## Проверка модуля

Тесты `tests/Sitko.Core.Pdf.Tests` вызывают настоящий браузер и поднимают контролируемый HTTP-сервер для HTML, CSS и изображения. После установки Chromium выполните:

```sh
dotnet test --project tests/Sitko.Core.Pdf.Tests/Sitko.Core.Pdf.Tests.csproj --framework net10.0
```

Для повтора через удалённый браузер задайте `PDF_TEST_BROWSER_WS_ENDPOINT`. Переменная `PDF_TEST_HTTP_HOST` задаёт адрес тестового сервера, доступный браузеру: например, `host.docker.internal` для браузера в Docker и тестов на macOS. По умолчанию используется `127.0.0.1`. Для явного локального исполняемого файла предназначена `PDF_TEST_BROWSER_EXECUTABLE_PATH`. Эти переменные относятся к тестовой оснастке, а конфигурация приложения использует секцию `PdfRenderer`.

Общий состав удалённых модулей и переход самостоятельной автоматизации браузера: [переход на версию 13](migration-v13.md).
