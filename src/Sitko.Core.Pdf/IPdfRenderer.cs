using Microsoft.Playwright;

namespace Sitko.Core.Pdf;

public interface IPdfRenderer
{
    Task<byte[]> GetPdfByUrlAsync(string url, PagePdfOptions? options = null, TimeSpan? delay = null);
    Task<byte[]> GetPdfByHtmlAsync(string html, PagePdfOptions? options = null, TimeSpan? delay = null);
    Task<byte[]> GetScreenshotByUrlAsync(string url, PageScreenshotOptions? options = null, TimeSpan? delay = null);
    Task<byte[]> GetScreenshotByHtmlAsync(string html, PageScreenshotOptions? options = null, TimeSpan? delay = null);
}

