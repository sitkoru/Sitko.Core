using System;
using System.Threading.Tasks;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.S3.Tests;

public class BaseS3StorageTestScope : BaseTestScope
{
    private readonly Guid bucketName = Guid.NewGuid();

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddS3Storage<TestS3StorageSettings>(moduleOptions =>
        {
            moduleOptions.Bucket = bucketName.ToString().ToLowerInvariant();
            moduleOptions.Prefix = "test";
        });
        application.AddS3StorageMetadata<TestS3StorageSettings>();

        return application;
    }

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        var storage = GetService<IStorage<TestS3StorageSettings>>();
        await storage.DeleteAllAsync();
    }
}
