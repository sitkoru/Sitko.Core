using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Xunit;

namespace Sitko.Core.Pdf.Tests;

public class PdfTest : BasePdfTest
{
    public PdfTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HtmlAndUrlContainRussianTextAndImage(bool fromUrl)
    {
        var scope = await GetScopeAsync();
        var renderer = scope.GetService<IPdfRenderer>();
        await using var server = await LocalPageServer.StartAsync();
        var bytes = fromUrl
            ? await renderer.GetPdfByUrlAsync(server.Url)
            : await renderer.GetPdfByHtmlAsync(server.Html);
        using var pdf = new PdfDocument(new PdfReader(new MemoryStream(bytes)));
        Assert.Contains("Проверка русского текста", PdfTextExtractor.GetTextFromPage(pdf.GetPage(1)));
        Assert.Contains("0 1 0 rg", Encoding.ASCII.GetString(pdf.GetPage(1).GetContentBytes()));
        var objects = pdf.GetPage(1).GetResources().GetResource(PdfName.XObject);
        Assert.NotNull(objects);
        Assert.Contains(objects.KeySet(), key => PdfName.Image.Equals(objects.GetAsStream(key)?.GetAsName(PdfName.Subtype)));
    }

    [Fact]
    public async Task PdfHonorsPageSize()
    {
        var scope = await GetScopeAsync();
        var renderer = scope.GetService<IPdfRenderer>();
        var bytes = await renderer.GetPdfByHtmlAsync("<p>Размер страницы</p>",
            new() { Width = "400px", Height = "600px" });
        using var pdf = new PdfDocument(new PdfReader(new MemoryStream(bytes)));
        Assert.Equal(300, pdf.GetPage(1).GetPageSize().GetWidth(), 1);
        Assert.Equal(450, pdf.GetPage(1).GetPageSize().GetHeight(), 1);
    }

    [Fact]
    public async Task CanRenderAgainAfterCleanupAndFailure()
    {
        var scope = await GetScopeAsync();
        var renderer = scope.GetService<IPdfRenderer>();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.GetPdfByUrlAsync("invalid-url"));
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var bytes = await renderer.GetPdfByHtmlAsync("<p>Повторная генерация</p>");
            using var pdf = new PdfDocument(new PdfReader(new MemoryStream(bytes)));
            Assert.Contains("Повторная генерация", PdfTextExtractor.GetTextFromPage(pdf.GetPage(1)));
        }
    }
}
