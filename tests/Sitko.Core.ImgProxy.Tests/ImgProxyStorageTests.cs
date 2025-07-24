using FluentAssertions;
using Sitko.Core.Storage;
using Sitko.Core.Storage.ImgProxy;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyStorageTests : BaseTest<ImgProxyStorageTestsScope>
{
    public ImgProxyStorageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    private static StorageItem TestItem { get; } = new("img/foo.png");

    [Fact]
    public async Task Url()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Url(TestItem);
        generated.Should()
            .Be(
                "https://imgproxy.test.com/SxQp7ZiYppbWLN0lA6-qCKCtEAafQidbWvGhObksdOg//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }

    [Fact]
    public async Task Format()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Format(TestItem, "jpg");
        generated.Should()
            .Be(
                "https://imgproxy.test.com/UrUQdqnjooZ8VB5f2p88GAAYsxAfMRad3JDDJpIoQEI//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc.jpg");
    }

    [Fact]
    public async Task Preset()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Preset(TestItem, "mypreset");
        generated.Should()
            .Be(
                "https://imgproxy.test.com/C6cOKi9dbUAS6tXyGMwgcikU9KekuG6jiOH2sb8nrSA/preset:mypreset/aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }

    [Fact]
    public async Task Build()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Build(TestItem, builder => builder.WithFormat("jpg"));
        generated.Should()
            .Be(
                "https://imgproxy.test.com/UrUQdqnjooZ8VB5f2p88GAAYsxAfMRad3JDDJpIoQEI//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc.jpg");
    }

    [Fact]
    public async Task Resize()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Resize(TestItem, 100, 100);
        generated.Should()
            .Be(
                "https://imgproxy.test.com/jUHPR01P3EetzCxdi8MX9cKbYJ7potdoEMO1NxZAIr4/resize:auto:100:100:0:0/aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }
}
