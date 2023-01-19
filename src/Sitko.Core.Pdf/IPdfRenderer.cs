using PuppeteerSharp;

namespace Sitko.Core.Pdf;

public interface IPdfRenderer
{
    Task<byte[]> GetPdfByUrlAsync(string url, PdfOptions? options = null, TimeSpan? delay = null);
    Task<byte[]> GetPdfByHtmlAsync(string html, PdfOptions? options = null, TimeSpan? delay = null);
    Task<byte[]> GetScreenshotByUrlAsync(string url, ScreenshotOptions? options = null, TimeSpan? delay = null);
    Task<byte[]> GetScreenshotByHtmlAsync(string html, ScreenshotOptions? options = null, TimeSpan? delay = null);
}

