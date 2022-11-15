using Sitko.Core.Storage.Remote.Tests.Server;
using Sitko.Core.Storage.S3;
using Sitko.Core.Xunit;
using Sitko.Core.Xunit.Web;

namespace Sitko.Core.Storage.Remote.Tests;

public class BaseRemoteStorageTestScope : WebTestScope
{
    private readonly Guid bucketName = Guid.NewGuid();

    protected override WebTestApplication ConfigureWebApplication(WebTestApplication application, string name)
    {
        base.ConfigureWebApplication(application, name);
        application.AddS3Storage<TestS3StorageSettings>(moduleOptions =>
            {
                moduleOptions.Bucket = bucketName.ToString().ToLowerInvariant();
                moduleOptions.Prefix = "test";
                var baseUrl = moduleOptions.Server!;
                var path = moduleOptions.Bucket;
                moduleOptions.PublicUri = new Uri(baseUrl, path + "/");
                moduleOptions.BucketPolicy = moduleOptions.AnonymousReadPolicy;
            })
            .AddS3StorageMetadata<TestS3StorageSettings>();
        return application;
    }

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddRemoteStorage<TestRemoteStorageSettings>(moduleOptions =>
        {
            moduleOptions.PublicUri = new Uri("https://localhost");
            if (Server is not null)
            {
                moduleOptions.RemoteUrl = new Uri(Server.BaseAddress, "Upload/");
                moduleOptions.HttpClientFactory = () =>
                {
                    var client = Server.CreateClient();
                    client.BaseAddress = new Uri(client.BaseAddress!, "Upload/");
                    return client;
                };
            }
        });

        return application;
    }
}

