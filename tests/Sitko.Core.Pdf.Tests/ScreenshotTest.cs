using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Pdf.Tests
{
    public class ScreenshotTest : BasePdfTest
    {
        public ScreenshotTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Url()
        {
            var scope = await GetScopeAsync();
            var renderer = scope.Get<IPdfRenderer>();

            var url = "https://github.com";
            var bytes = await renderer.GetScreenshotByUrlAsync(url);
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public async Task Pdf()
        {
            var scope = await GetScopeAsync();
            var renderer = scope.Get<IPdfRenderer>();

            var html =
                "<html lang=\"en\">\n<head>\n    <title>Title</title>\n</head>\n<body>\n<h1>Hello, World!</h1>\n</body>\n</html>";
            var bytes = await renderer.GetScreenshotByHtmlAsync(html);
            Assert.NotEmpty(bytes);
        }
    }
}
