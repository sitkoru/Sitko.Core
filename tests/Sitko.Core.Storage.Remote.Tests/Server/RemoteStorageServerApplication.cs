using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Sitko.Core.App.Web;
using Sitko.Core.Storage.S3;

namespace Sitko.Core.Storage.Remote.Tests.Server;

public class RemoteStorageServerApplication : WebApplication<RemoteStorageServerStartup>
{
    private Guid bucketName = Guid.NewGuid();

    public RemoteStorageServerApplication(string[] args) : base(args) =>
        this.AddS3Storage<TestS3StorageSettings>(moduleOptions =>
            {
                moduleOptions.Bucket = bucketName.ToString().ToLowerInvariant();
                moduleOptions.Prefix = "test";
                var baseUrl = moduleOptions.Server!;
                var path = moduleOptions.Bucket;
                moduleOptions.PublicUri = new Uri(baseUrl, path + "/");
                moduleOptions.BucketPolicy = moduleOptions.AnonymousReadPolicy;
            })
            .AddS3StorageMetadata<TestS3StorageSettings>();

    protected override void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
    {
        base.ConfigureWebHostDefaults(webHostBuilder);
        webHostBuilder.UseTestServer();
    }
}
