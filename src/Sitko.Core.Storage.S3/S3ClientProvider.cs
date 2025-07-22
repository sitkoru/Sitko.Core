using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Storage.S3;

public class S3ClientProvider(IServiceProvider serviceProvider)
{
    public IAmazonS3 GetS3Client<TS3StorageOptions>() where TS3StorageOptions : S3StorageOptions, new() =>
        serviceProvider.GetRequiredKeyedService<IAmazonS3>(typeof(TS3StorageOptions).Name);
}
