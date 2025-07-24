using FluentAssertions;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyTests : BaseTest<ImgProxyTestsScope>
{
    private const string TestUrl = "https://img.test.com/img/foo.png";

    public ImgProxyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Url()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator>();

        var generated = generator.Url(TestUrl);
        generated.Should()
            .Be(
                "https://imgproxy.test.com/SxQp7ZiYppbWLN0lA6-qCKCtEAafQidbWvGhObksdOg//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }

    [Fact]
    public async Task Format()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator>();

        var generated = generator.Format(TestUrl, "jpg");
        generated.Should()
            .Be(
                "https://imgproxy.test.com/UrUQdqnjooZ8VB5f2p88GAAYsxAfMRad3JDDJpIoQEI//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc.jpg");
    }

    [Fact]
    public async Task Preset()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator>();

        var url = "https://img.test.com/img/foo.png";
        var generated = generator.Preset(url, "mypreset");
        generated.Should()
            .Be(
                "https://imgproxy.test.com/C6cOKi9dbUAS6tXyGMwgcikU9KekuG6jiOH2sb8nrSA/preset:mypreset/aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }

    [Fact]
    public async Task Build()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator>();

        var generated = generator.Build(TestUrl, builder => builder.WithFormat("jpg"));
        generated.Should()
            .Be(
                "https://imgproxy.test.com/UrUQdqnjooZ8VB5f2p88GAAYsxAfMRad3JDDJpIoQEI//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc.jpg");
    }

    [Fact]
    public async Task Resize()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator>();

        var generated = generator.Resize(TestUrl, 100, 100);
        generated.Should()
            .Be(
                "https://imgproxy.test.com/jUHPR01P3EetzCxdi8MX9cKbYJ7potdoEMO1NxZAIr4/resize:auto:100:100:0:0/aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }
}
