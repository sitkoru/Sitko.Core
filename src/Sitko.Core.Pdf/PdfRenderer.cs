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

        public async Task<byte[]> GetPdfByUrlAsync(string url, PdfOptions? options = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                var page = await browser.NewPageAsync();
                await page.GoToAsync(url);
                options ??= GetDefaultOptions();
                var pdf = await page.PdfDataAsync(options);
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from url: {errorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
        }

        public PdfOptions GetDefaultOptions()
        {
            return new PdfOptions {PrintBackground = true};
        }

        public async Task<byte[]> GetPdfByHtmlAsync(string html, PdfOptions? options = null)
        {
            try
            {
                await using var browser = await GetBrowserAsync();
                await using var page = await browser.NewPageAsync();
                await page.SetContentAsync(html);
                options ??= GetDefaultOptions();
                var pdf = await page.PdfDataAsync(options);
                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating pdf from html: {errorText}", ex.ToString());
                throw new Exception(ex.Message, ex);
            }
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
