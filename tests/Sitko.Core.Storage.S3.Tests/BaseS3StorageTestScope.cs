using Microsoft.Extensions.Hosting;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.S3.Tests;

public class BaseS3StorageTestScope : BaseTestScope
{
    private readonly Guid bucketName = Guid.NewGuid();

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddS3Storage<TestS3StorageSettings>(moduleOptions =>
            {
                moduleOptions.Bucket = bucketName.ToString().ToLowerInvariant();
                moduleOptions.Prefix = "test";
                moduleOptions.DeleteBucketOnCleanup = true;
            })
            .AddS3StorageMetadata<TestS3StorageSettings>();

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        var storage = GetService<IStorage<TestS3StorageSettings>>();
        await storage.DeleteAllAsync();
    }
}
