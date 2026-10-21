using System.Buffers.Binary;
using Xunit;

namespace Sitko.Core.Pdf.Tests;

public class ScreenshotTest : BasePdfTest
{
    public ScreenshotTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Url()
    {
        var scope = await GetScopeAsync();
        var renderer = scope.GetService<IPdfRenderer>();

        var url = "https://github.com";
        var bytes = await renderer.GetScreenshotByUrlAsync(url);
        Assert.True(bytes.Length >= 24);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
        Assert.True(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)) > 0);
        Assert.True(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)) > 0);
    }

    [Fact]
    public async Task Pdf()
    {
        var scope = await GetScopeAsync();
        var renderer = scope.GetService<IPdfRenderer>();

        var html =
            "<html lang=\"en\">\n<head>\n    <title>Title</title>\n</head>\n<body>\n<h1>Hello, World!</h1>\n</body>\n</html>";
        var bytes = await renderer.GetScreenshotByHtmlAsync(html);
        Assert.True(bytes.Length >= 24);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
        Assert.True(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)) > 0);
        Assert.True(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)) > 0);
    }
}
