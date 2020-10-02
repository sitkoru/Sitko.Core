using System;
using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Pdf.Tests
{
    public class PdfTest : BasePdfTest
    {
        public PdfTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Url()
        {
            var scope = await GetScopeAsync();
            var renderer = scope.Get<IPdfRenderer>();

            var url = "https://github.com";
            var bytes = await renderer.GetPdfByUrlAsync(url);
            Assert.NotEmpty(bytes);
            
            using var pdf = new PdfDocument(new PdfReader(new MemoryStream(bytes)));
            var text = PdfTextExtractor.GetTextFromPage(pdf.GetPage(1), new LocationTextExtractionStrategy());
            pdf.Close();

            Assert.Contains("Built for developers", text);
        }

        [Fact]
        public async Task PdfAsync()
        {
            var scope = await GetScopeAsync();
            var renderer = scope.Get<IPdfRenderer>();

            var html =
                "<html lang=\"en\">\n<head>\n    <title>Title</title>\n</head>\n<body>\n<h1>Hello, World!</h1>\n</body>\n</html>";
            var bytes = await renderer.GetPdfByHtmlAsync(html);
            Assert.NotEmpty(bytes);
            
            using var pdf = new PdfDocument(new PdfReader(new MemoryStream(bytes)));
            var text = PdfTextExtractor.GetTextFromPage(pdf.GetPage(1), new LocationTextExtractionStrategy());
            pdf.Close();

            Assert.Contains("Hello, World!", text);
        }
    }
}
