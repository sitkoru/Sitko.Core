using Sitko.Core.Storage.Remote.Tests.Server;
using Sitko.Core.Storage.S3;
using Sitko.Core.Xunit.Web;

namespace Sitko.Core.Storage.Remote.Tests;

public class BaseRemoteStorageTestScope : WebTestScope
{
    private readonly Guid bucketName = Guid.NewGuid();

    protected override WebApplicationBuilder ConfigureWebApplication(WebApplicationBuilder webApplicationBuilder,
        string name)
    {
        base.ConfigureWebApplication(webApplicationBuilder, name).AddS3Storage<TestS3StorageSettings>(
                moduleOptions =>
                {
                    moduleOptions.Bucket = bucketName.ToString().ToLowerInvariant();
                    moduleOptions.Prefix = "test";
                    var baseUrl = moduleOptions.Server!;
                    var path = moduleOptions.Bucket;
                    moduleOptions.PublicUri = new Uri(baseUrl, path + "/");
                    moduleOptions.BucketPolicy = moduleOptions.AnonymousReadPolicy;
                })
            .AddS3StorageMetadata<TestS3StorageSettings>();
        return webApplicationBuilder;
    }

    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name).AddRemoteStorage<TestRemoteStorageSettings>(moduleOptions =>
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
}
