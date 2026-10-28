# PDF and page screenshots

[Русский](pdf.md)

`Sitko.Core.Pdf` renders PDF documents and page screenshots from HTML or URLs using `Microsoft.Playwright` and Chromium. It supports launching an installed local browser and connecting to a remote browser through the Chrome DevTools Protocol (CDP).

## Registration and usage

Register Sitko.Core, call `AddPdfRenderer()`, and resolve `IPdfRenderer` from dependency injection:

```csharp
builder.AddSitkoCore().AddPdfRenderer();
// After building and starting the application:
var renderer = host.Services.GetRequiredService<IPdfRenderer>();
var pdf = await renderer.GetPdfByHtmlAsync("<html lang=\"en\"><body>Invoice 13</body></html>");
```

Import `Sitko.Core.App`, `Sitko.Core.Pdf`, and `Microsoft.Extensions.DependencyInjection`. The example assumes a `HostApplicationBuilder builder` and an `IHost host` built from it.

| Method | Result | Options |
| --- | --- | --- |
| `GetPdfByHtmlAsync` / `GetPdfByUrlAsync` | PDF `byte[]` | `PagePdfOptions?`, `TimeSpan? delay` |
| `GetScreenshotByHtmlAsync` / `GetScreenshotByUrlAsync` | Screenshot `byte[]` | `PageScreenshotOptions?`, `TimeSpan? delay` |

Option types belong to `Microsoft.Playwright`. With `options == null`, PDF includes backgrounds and screenshots use PNG and capture the full page. An explicitly supplied options object is used as provided; set `PrintBackground`, `FullPage`, and `Type` explicitly when needed. `delay` adds a wait after HTML/page loading; it does not establish readiness of arbitrary asynchronous charts.

## Browser configuration

The configuration section is `PdfRenderer`. You can also configure it through `AddPdfRenderer(options => ...)`; overloads accept the application context and a custom configuration key.

| Setting | Default | Meaning |
| --- | --- | --- |
| `BrowserWsEndpoint` | unset | Connect through CDP when set; otherwise launch locally |
| `ExecutablePath` | unset | Explicit local Chromium executable; otherwise use the browser installed for this Playwright version |
| `AcceptInsecureCerts` | `false` | Ignore HTTPS errors in the request context |
| `ViewportSize` | `800 × 600` | Page viewport |
| `BrowserArgs` | `["--no-sandbox"]` | Additional local launch arguments |
| `Headless` | `true` | Launch the local browser without a UI |

Remote browser example:

```json
{
  "PdfRenderer": {
    "BrowserWsEndpoint": "ws://browserless:3000/chromium?token=example"
  }
}
```

The corresponding environment variable is `PdfRenderer__BrowserWsEndpoint`. Host, port, path, and authentication depend on the selected CDP server; replace the example token with the environment's configuration.

Each operation owns its browser context. Completion releases the context and the local browser or remote connection. The renderer does not terminate the remote browser. Rendering failures are exposed as `InvalidOperationException` with the original exception in `InnerException`.

## Preparing the environment

Local execution requires Chromium, operating system libraries, and fonts covering the document's characters. After building the application, install the browser using **the same Playwright version**, for example:

```sh
pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

The example path is relative to the application build; select the actual configuration and target framework. Install during environment/image preparation. The browser cache must be accessible to the application user. The renderer does not download Chromium during requests. Preserve the Playwright driver files supplied by the package when publishing.

Remote mode does not require local Chromium, but the application still needs the Playwright driver. The browser needs network access to the document URL and its CSS, images, and fonts. In HTML, `localhost` refers to the browser's environment. Verify CDP server compatibility when updating the library or browser image.

See [browser installation](https://playwright.dev/dotnet/docs/browsers) and [CDP connections](https://playwright.dev/dotnet/docs/api/class-browsertype#browser-type-connect-over-cdp).

## Migrating to Sitko.Core 13

1. Remove `AddPuppeteer()` and `using Sitko.Core.Puppeteer` when used only for PDF. Keep `AddPdfRenderer()`.
2. Replace `PuppeteerSharp.PdfOptions` with `Microsoft.Playwright.PagePdfOptions` and `ScreenshotOptions` with `PageScreenshotOptions`. Map option properties using the Playwright API; the types are not all one-to-one renames.
3. Move browser configuration from `Puppeteer` to `PdfRenderer`. Replace `ViewPortOptions` with `ViewportSize`. `Product` and `Revision` are unsupported: PDF uses Chromium, whose version is installed with the environment. Replace `PUPPETEER_EXECUTABLE_PATH` with `PdfRenderer__ExecutablePath` when an explicit executable is needed.
4. Prepare the browser environment and verify documents with their actual fonts and resources. Calls without options, such as `GetPdfByHtmlAsync(html)`, retain their shape after recompilation.

Implementation: [interface](../src/Sitko.Core.Pdf/IPdfRenderer.cs), [renderer](../src/Sitko.Core.Pdf/PdfRenderer.cs), [settings](../src/Sitko.Core.Pdf/PdfRendererModuleOptions.cs). Contract: [specification](../specs/20261027-150000-pdf-rendering/spec.md).

## Testing the module

The `tests/Sitko.Core.Pdf.Tests` suite uses a real browser and a controlled HTTP server for HTML, CSS, and an image. After installing Chromium, run:

```sh
dotnet test tests/Sitko.Core.Pdf.Tests/Sitko.Core.Pdf.Tests.csproj --framework net10.0
```

Set `PDF_TEST_BROWSER_WS_ENDPOINT` to repeat the suite with a remote browser. `PDF_TEST_HTTP_HOST` selects the fixture server address reachable from the browser, for example `host.docker.internal` for a browser in Docker and tests on macOS. The default is `127.0.0.1`. `PDF_TEST_BROWSER_EXECUTABLE_PATH` selects an explicit local executable. These variables belong to the test fixture; application configuration uses the `PdfRenderer` section.

For the full removed-module list and standalone browser automation migration, see [migration to version 13](migration-v13.en.md).
