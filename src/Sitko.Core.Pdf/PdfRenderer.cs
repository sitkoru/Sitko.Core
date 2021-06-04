using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Sitko.Core.Pdf
{
    internal class PdfRenderer : IPdfRenderer
    {
        private readonly PdfOptions _defaultOptions = new() {PrintBackground = true};

        private readonly ScreenshotOptions _defaultScreenshotOptions =
            new() {FullPage = true, Type = ScreenshotType.Png};

        private readonly ILogger<PdfRenderer> _logger;
        private readonly IOptionsMonitor<PdfRendererModuleOptions> _optionsMonitor;

        public PdfRenderer(IOptionsMonitor<PdfRendererModuleOptions> optionsMonitor, ILogger<PdfRenderer> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        private PdfRendererModuleOptions Options => _optionsMonitor.CurrentValue;

        public async Task<byte[]> GetPdfByUrlAsync(string url, PdfOptions? options = null, TimeSpan? delay = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                var page = await GetPageByUrl(browser, url, delay);
                options ??= _defaultOptions;
                var pdf = await page.PdfDataAsync(options);
                await browser.CloseAsync();
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<byte[]> GetPdfByHtmlAsync(string html, PdfOptions? options = null, TimeSpan? delay = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                var page = await GetPageWithHtml(browser, html, delay);
                options ??= _defaultOptions;
                var pdf = await page.PdfDataAsync(options);
                await browser.CloseAsync();
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from html: {ErrorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<byte[]> GetScreenshotByUrlAsync(string url, ScreenshotOptions? options = null,
            TimeSpan? delay = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                var page = await GetPageByUrl(browser, url, delay);
                options ??= _defaultScreenshotOptions;
                var screenshot = await page.ScreenshotDataAsync(options);
                await browser.CloseAsync();
                return screenshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<byte[]> GetScreenshotByHtmlAsync(string html, ScreenshotOptions? options = null,
            TimeSpan? delay = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                var page = await GetPageWithHtml(browser, html, delay);
                options ??= _defaultScreenshotOptions;
                var screenshot = await page.ScreenshotDataAsync(options);
                await browser.CloseAsync();
                return screenshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        private async Task<Page> GetPageByUrl(Browser browser, string url, TimeSpan? delay = null)
        {
            var page = await browser.NewPageAsync();
            await page.GoToAsync(url);
            if (delay != null)
            {
                await Task.Delay(delay.Value);
            }

            return page;
        }

        private async Task<Page> GetPageWithHtml(Browser browser, string html, TimeSpan? delay = null)
        {
            var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            if (delay != null)
            {
                await Task.Delay(delay.Value);
            }

            return page;
        }

        private async Task<Browser> GetBrowserAsync()
        {
            _logger.LogInformation("Start new Browser");
            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] {"--no-sandbox"},
                IgnoreHTTPSErrors = Options.IgnoreHTTPSErrors,
                DefaultViewport = Options.ViewPortOptions
            });
        }
    }
}
