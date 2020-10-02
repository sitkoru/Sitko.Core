using System.Threading.Tasks;
using PuppeteerSharp;

namespace Sitko.Core.Pdf
{
    public interface IPdfRenderer
    {
        Task<byte[]> GetPdfByUrlAsync(string url, PdfOptions? options = null);
        Task<byte[]> GetPdfByHtmlAsync(string html, PdfOptions? options = null);
    }
}
