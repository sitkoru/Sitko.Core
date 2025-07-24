using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;
using Xunit;

namespace Sitko.Core.Storage.FileSystem.Tests;

public class MultipleStorageTests : BaseTest<MultipleStorageTestsScope>
{
    public MultipleStorageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Multiple()
    {
        var scope = await GetScopeAsync();
        var storages = scope.GetServices<IStorage>().ToList();
        Assert.NotEmpty(storages);
        Assert.Equal(3, storages.Count);
    }

    [Fact]
    public async Task Default()
    {
        var scope = await GetScopeAsync();
        var storage = scope.GetService<IStorage>();
        Assert.NotNull(storage);
        Assert.IsAssignableFrom<IStorage<MultipleStorageTestsOptionsSecond>>(storage);
    }

    [Fact]
    public async Task Specific()
    {
        var scope = await GetScopeAsync();
        var storage = scope.GetService<IStorage<MultipleStorageTestsOptionsThird>>();
        Assert.NotNull(storage);
    }
}

[UsedImplicitly]
public class MultipleStorageTestsScope : BaseFileSystemStorageTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddFileSystemStorage<MultipleStorageTestsOptionsSecond>(moduleOptions =>
            {
                var folder = Path.GetTempPath() + "/" + Guid.NewGuid();
                moduleOptions.PublicUri = new Uri(folder);
                moduleOptions.StoragePath = folder;
                moduleOptions.IsDefault = true;
            })
            .AddFileSystemStorageMetadata<MultipleStorageTestsOptionsSecond>()
            .AddFileSystemStorage<MultipleStorageTestsOptionsThird>(moduleOptions =>
            {
                var folder = Path.GetTempPath() + "/" + Guid.NewGuid();
                moduleOptions.PublicUri = new Uri(folder);
                moduleOptions.StoragePath = folder;
            })
            .AddFileSystemStorageMetadata<MultipleStorageTestsOptionsThird>();
}

public class MultipleStorageTestsOptionsSecond : StorageOptions, IFileSystemStorageOptions
{
    public string StoragePath { get; set; } = "/tmp/storage/second";
}

public class MultipleStorageTestsOptionsThird : StorageOptions, IFileSystemStorageOptions
{
    public string StoragePath { get; set; } = "/tmp/storage/third";
}
