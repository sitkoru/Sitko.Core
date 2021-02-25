using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Sitko.Core.Pdf
{
    internal class PdfRenderer : IPdfRenderer
    {
        private readonly PdfRendererModuleConfig _config;
        private readonly ILogger<PdfRenderer> _logger;

        public PdfRenderer(PdfRendererModuleConfig config, ILogger<PdfRenderer> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<byte[]> GetPdfByUrlAsync(string url, PdfOptions? options = null, TimeSpan? delay = null)
        {
            try
            {
                await using var page = await GetPageByUrl(url, delay);
                options ??= GetDefaultOptions();
                var pdf = await page.PdfDataAsync(options);
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        private async Task<Page> GetPageByUrl(string url, TimeSpan? delay = null)
        {
            await using var browser = await GetBrowserAsync();
            var page = await browser.NewPageAsync();
            await page.GoToAsync(url);
            if (delay != null)
            {
                await Task.Delay(delay.Value);
            }

            return page;
        }

        public async Task<byte[]> GetPdfByHtmlAsync(string html, PdfOptions? options = null, TimeSpan? delay = null)
        {
            try
            {
                await using var page = await GetPageWithHtml(html, delay);
                options ??= GetDefaultOptions();
                var pdf = await page.PdfDataAsync(options);
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
                await using var page = await GetPageByUrl(url, delay);
                options ??= GetDefaultScreenshotOptions();
                return await page.ScreenshotDataAsync(options);
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
                await using var page = await GetPageWithHtml(html, delay);
                options ??= GetDefaultScreenshotOptions();
                return await page.ScreenshotDataAsync(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from url: {ErrorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        private async Task<Page> GetPageWithHtml(string html, TimeSpan? delay = null)
        {
            await using var browser = await GetBrowserAsync();
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            if (delay != null)
            {
                await Task.Delay(delay.Value);
            }

            return page;
        }

        private PdfOptions GetDefaultOptions()
        {
            return new() {PrintBackground = true};
        }

        private ScreenshotOptions GetDefaultScreenshotOptions()
        {
            return new() {FullPage = true, Type = ScreenshotType.Png};
        }

        private async Task<Browser> GetBrowserAsync()
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true, Args = new[] {"--no-sandbox"}, IgnoreHTTPSErrors = _config.IgnoreHTTPSErrors
            });
        }
    }
}
