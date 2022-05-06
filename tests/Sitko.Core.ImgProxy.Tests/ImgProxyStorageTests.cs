using System.Threading.Tasks;
using FluentAssertions;
using Sitko.Core.Storage;
using Sitko.Core.Storage.ImgProxy;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.ImgProxy.Tests;

public class ImgProxyStorageTests : BaseTest<ImgProxyStorageTestsScope>
{
    private static readonly StorageItem testItem = new("img/foo.png");

    public ImgProxyStorageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Url()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Url(testItem);
        generated.Should()
            .Be(
                "https://imgproxy.test.com/SxQp7ZiYppbWLN0lA6-qCKCtEAafQidbWvGhObksdOg//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }

    [Fact]
    public async Task Format()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Format(testItem, "jpg");
        generated.Should()
            .Be(
                "https://imgproxy.test.com/UrUQdqnjooZ8VB5f2p88GAAYsxAfMRad3JDDJpIoQEI//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc.jpg");
    }

    [Fact]
    public async Task Preset()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Preset(testItem, "mypreset");
        generated.Should()
            .Be(
                "https://imgproxy.test.com/C6cOKi9dbUAS6tXyGMwgcikU9KekuG6jiOH2sb8nrSA/preset:mypreset/aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }

    [Fact]
    public async Task Build()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Build(testItem, builder => builder.WithFormat("jpg"));
        generated.Should()
            .Be(
                "https://imgproxy.test.com/UrUQdqnjooZ8VB5f2p88GAAYsxAfMRad3JDDJpIoQEI//aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc.jpg");
    }

    [Fact]
    public async Task Resize()
    {
        var scope = await GetScopeAsync();
        var generator = scope.GetService<IImgProxyUrlGenerator<ImgProxyFileSystemStorageSettings>>();

        var generated = generator.Resize(testItem, 100, 100);
        generated.Should()
            .Be(
                "https://imgproxy.test.com/jUHPR01P3EetzCxdi8MX9cKbYJ7potdoEMO1NxZAIr4/resize:auto:100:100:0:0/aHR0cHM6Ly9pbWcudGVzdC5jb20vaW1nL2Zvby5wbmc");
    }
}
