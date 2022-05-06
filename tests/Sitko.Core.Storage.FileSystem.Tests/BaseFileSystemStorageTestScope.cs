using System;
using System.IO;
using System.Threading.Tasks;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.FileSystem.Tests;

public class BaseFileSystemStorageTestScope : BaseTestScope
{
    private readonly string folder = Path.GetTempPath() + "/" + Guid.NewGuid();

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddFileSystemStorage<TestFileSystemStorageSettings>(
            moduleOptions =>
            {
                moduleOptions.PublicUri = new Uri(folder);
                moduleOptions.StoragePath = folder;
            });
        application.AddFileSystemStorageMetadata<TestFileSystemStorageSettings>();
        return application;
    }

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        var storage = GetService<IStorage<TestFileSystemStorageSettings>>();
        await storage.DeleteAllAsync();
    }
}
