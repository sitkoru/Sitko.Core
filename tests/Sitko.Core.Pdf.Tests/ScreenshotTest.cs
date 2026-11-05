using System.Buffers.Binary;
using Xunit;

namespace Sitko.Core.Pdf.Tests;

public class ScreenshotTest : BasePdfTest
{
    public ScreenshotTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HtmlAndUrlProduceFullPagePngWithExternalCss(bool fromUrl)
    {
        var scope = await GetScopeAsync();
        var renderer = scope.GetService<IPdfRenderer>();
        await using var server = await LocalPageServer.StartAsync();
        var bytes = fromUrl
            ? await renderer.GetScreenshotByUrlAsync(server.Url)
            : await renderer.GetScreenshotByHtmlAsync(server.Html);
        AssertPngSize(bytes, 800, 1100);
    }

    [Fact]
    public async Task ScreenshotHonorsClipSize()
    {
        var scope = await GetScopeAsync();
        var renderer = scope.GetService<IPdfRenderer>();
        var bytes = await renderer.GetScreenshotByHtmlAsync("<p>Размер снимка</p>",
            new() { Clip = new() { X = 0, Y = 0, Width = 123, Height = 77 } });
        AssertPngSize(bytes, 123, 77);
    }

    private static void AssertPngSize(byte[] bytes, uint width, uint height)
    {
        Assert.True(bytes.Length >= 24);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
        Assert.Equal(width, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)));
        Assert.Equal(height, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)));
    }
}
