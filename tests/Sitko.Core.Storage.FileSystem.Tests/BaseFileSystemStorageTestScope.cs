using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.FileSystem.Tests;

public class BaseFileSystemStorageTestScope : BaseTestScope
{
    private readonly string folder = Path.GetTempPath() + "/" + Guid.NewGuid();

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddFileSystemStorage<TestFileSystemStorageSettings>(
                moduleOptions =>
                {
                    moduleOptions.PublicUri = new Uri(folder);
                    moduleOptions.StoragePath = folder;
                })
            .AddFileSystemStorageMetadata<TestFileSystemStorageSettings>();

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        var storage = GetService<IStorage<TestFileSystemStorageSettings>>();
        await storage.DeleteAllAsync();
    }
}
