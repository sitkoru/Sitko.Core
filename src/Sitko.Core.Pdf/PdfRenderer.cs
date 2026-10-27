using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Sitko.Core.Pdf;

internal sealed class PdfRenderer(
    IOptionsMonitor<PdfRendererModuleOptions> optionsMonitor,
    ILogger<PdfRenderer> logger) : IPdfRenderer
{
    public Task<byte[]> GetPdfByUrlAsync(string url, PagePdfOptions? options = null, TimeSpan? delay = null) =>
        RenderAsync(page => page.GotoAsync(url), page => page.PdfAsync(options ?? new() { PrintBackground = true }), delay);

    public Task<byte[]> GetPdfByHtmlAsync(string html, PagePdfOptions? options = null, TimeSpan? delay = null) =>
        RenderAsync(page => page.SetContentAsync(html), page => page.PdfAsync(options ?? new() { PrintBackground = true }), delay);

    public Task<byte[]> GetScreenshotByUrlAsync(string url, PageScreenshotOptions? options = null, TimeSpan? delay = null) =>
        RenderAsync(page => page.GotoAsync(url),
            page => page.ScreenshotAsync(options ?? new() { FullPage = true, Type = ScreenshotType.Png }), delay);

    public Task<byte[]> GetScreenshotByHtmlAsync(string html, PageScreenshotOptions? options = null, TimeSpan? delay = null) =>
        RenderAsync(page => page.SetContentAsync(html),
            page => page.ScreenshotAsync(options ?? new() { FullPage = true, Type = ScreenshotType.Png }), delay);

    private async Task<byte[]> RenderAsync(Func<IPage, Task> load, Func<IPage, Task<byte[]>> render, TimeSpan? delay)
    {
        try
        {
            var options = optionsMonitor.CurrentValue;
            using var playwright = await Playwright.CreateAsync();
            await using var browser = string.IsNullOrEmpty(options.BrowserWsEndpoint)
                ? await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = options.Headless,
                    Args = options.BrowserArgs,
                    ExecutablePath = options.ExecutablePath
                })
                : await playwright.Chromium.ConnectOverCDPAsync(options.BrowserWsEndpoint);
            await using var context = await browser.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = options.AcceptInsecureCerts,
                ViewportSize = options.ViewportSize
            });
            var page = await context.NewPageAsync();
            await load(page);
            if (delay != null)
            {
                await Task.Delay(delay.Value);
            }

            return await render(page);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while rendering document: {ErrorText}", ex.Message);
            throw new InvalidOperationException(ex.Message, ex);
        }
    }
}
