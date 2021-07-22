using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Sitko.Core.Pdf
{
    internal class PdfRenderer : IPdfRenderer
    {
        private readonly PdfOptions defaultOptions = new() {PrintBackground = true};

        private readonly ScreenshotOptions defaultScreenshotOptions =
            new() {FullPage = true, Type = ScreenshotType.Png};

        private readonly ILogger<PdfRenderer> logger;
        private readonly IOptionsMonitor<PdfRendererModuleOptions> optionsMonitor;
        private readonly ILoggerFactory loggerFactory;

        public PdfRenderer(IOptionsMonitor<PdfRendererModuleOptions> optionsMonitor, ILoggerFactory loggerFactory,
            ILogger<PdfRenderer> logger)
        {
            this.optionsMonitor = optionsMonitor;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        private PdfRendererModuleOptions Options => optionsMonitor.CurrentValue;

        public async Task<byte[]> GetPdfByUrlAsync(string url, PdfOptions? options = null, TimeSpan? delay = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                var page = await GetPageByUrl(browser, url, delay);
                options ??= defaultOptions;
                var pdf = await page.PdfDataAsync(options);
                await browser.CloseAsync();
                return pdf;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<byte[]> GetPdfByHtmlAsync(string html, PdfOptions? options = null, TimeSpan? delay = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                var page = await GetPageWithHtml(browser, html, delay);
                options ??= defaultOptions;
                var pdf = await page.PdfDataAsync(options);
                await browser.CloseAsync();
                return pdf;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while generating pdf from html: {ErrorText}", ex.ToString());
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
                options ??= defaultScreenshotOptions;
                var screenshot = await page.ScreenshotDataAsync(options);
                await browser.CloseAsync();
                return screenshot;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
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
                options ??= defaultScreenshotOptions;
                var screenshot = await page.ScreenshotDataAsync(options);
                await browser.CloseAsync();
                return screenshot;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
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
            logger.LogInformation("Start new Browser");
            if (!string.IsNullOrEmpty(Options.BrowserWsEndpoint))
            {
                return await Puppeteer.ConnectAsync(
                    new ConnectOptions
                    {
                        BrowserWSEndpoint = Options.BrowserWsEndpoint,
                        IgnoreHTTPSErrors = Options.IgnoreHTTPSErrors,
                        DefaultViewport = Options.ViewPortOptions
                    },
                    loggerFactory);
            }

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
